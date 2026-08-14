#!/usr/bin/env node
/**
 * Architecture guard for the Clean Architecture + vertical-slice layout:
 *   R1 every csproj's ProjectReference/PackageReference NAMES exactly match
 *      architecture/dependencies.json (versions stay free in the csproj)
 *   R2 in src/POS.API only Program.cs may mention POS.Infrastructure or AppDbContext
 *   R3 src/POS.Application follows the slice grammar:
 *      DependencyInjection.cs | Common/** | <Module>/{Commands,Queries,EventHandlers}/...
 * Zero-dependency; csproj parsed with regexes; obj/ and bin/ skipped.
 */
import { readdirSync, readFileSync } from 'node:fs';
import { join, relative, sep } from 'node:path';
import { fileURLToPath } from 'node:url';

const root = fileURLToPath(new URL('..', import.meta.url));
const lock = JSON.parse(readFileSync(join(root, 'architecture', 'dependencies.json'), 'utf8'));

const SKIP_DIRS = new Set(['obj', 'bin']);
function* walk(dir) {
  for (const entry of readdirSync(dir, { withFileTypes: true })) {
    if (entry.isDirectory()) {
      if (SKIP_DIRS.has(entry.name)) continue;
      yield* walk(join(dir, entry.name));
    } else {
      yield join(dir, entry.name);
    }
  }
}

const rel = (path) => relative(root, path).split(sep).join('/');
const hits = [];

// ---- R1: dependency lock ----
const foundCsproj = new Set();
for (const top of ['src', 'tests']) {
  for (const file of walk(join(root, top))) {
    if (file.endsWith('.csproj')) foundCsproj.add(rel(file));
  }
}
for (const csproj of foundCsproj) {
  if (!(csproj in lock)) {
    hits.push(`${csproj}: R1 csproj not in architecture/dependencies.json — add it there in the same PR`);
  }
}
for (const [csproj, expected] of Object.entries(lock)) {
  if (!foundCsproj.has(csproj)) {
    hits.push(`${csproj}: R1 listed in architecture/dependencies.json but missing from the repo`);
    continue;
  }
  const xml = readFileSync(join(root, csproj), 'utf8');
  const projects = [...xml.matchAll(/<ProjectReference\s+Include="([^"]+)"/g)]
    .map(([, path]) => path.split(/[\\/]/).pop().replace(/\.csproj$/, ''));
  const packages = [...xml.matchAll(/<PackageReference\s+Include="([^"]+)"/g)].map(([, name]) => name);
  for (const [kind, actual, wanted] of [
    ['project', projects, expected.projects],
    ['package', packages, expected.packages],
  ]) {
    for (const name of wanted) {
      if (!actual.includes(name)) {
        hits.push(`${csproj}: R1 missing ${kind} reference "${name}" (in the lock, not the csproj)`);
      }
    }
    for (const name of actual) {
      if (!wanted.includes(name)) {
        hits.push(`${csproj}: R1 extra ${kind} reference "${name}" — if deliberate, add it to architecture/dependencies.json in the same PR`);
      }
    }
  }
}

// ---- R2: composition root ----
const apiDir = join(root, 'src', 'POS.API');
for (const file of walk(apiDir)) {
  if (!file.endsWith('.cs') || file === join(apiDir, 'Program.cs')) continue;
  const lines = readFileSync(file, 'utf8').split(/\r?\n/);
  lines.forEach((text, index) => {
    if (/\bPOS\.Infrastructure\b|\bAppDbContext\b/.test(text)) {
      hits.push(`${rel(file)}:${index + 1}: R2 references POS.Infrastructure/AppDbContext outside Program.cs — controllers stay thin MediatR dispatchers`);
    }
  });
}

// ---- R3: slice grammar ----
const appDir = join(root, 'src', 'POS.Application');
for (const file of walk(appDir)) {
  const parts = relative(appDir, file).split(sep);
  if (parts.length === 1 && (parts[0] === 'DependencyInjection.cs' || parts[0] === 'POS.Application.csproj')) continue;
  if (parts[0] === 'Common') continue;
  const kind = parts[1];
  let ok = false;
  if (kind === 'EventHandlers') {
    ok = parts.length === 3 && parts[2].endsWith('EventHandler.cs');
  } else if (kind === 'Commands' || kind === 'Queries') {
    const noun = kind === 'Commands' ? 'Command' : 'Query';
    const action = parts[2];
    ok = parts.length === 4 && new RegExp(`^${action}${noun}(Handler|Validator)?\\.cs$`).test(parts[3]);
  }
  if (!ok) {
    hits.push(`${rel(file)}: R3 breaks the slice grammar — <Module>/{Commands,Queries,EventHandlers}/<Action>/<Action>{Command|Query}{,Handler,Validator}.cs (Common/ is free-form)`);
  }
}

if (hits.length > 0) {
  console.error('check:architecture FAILED — Clean Architecture rules violated\n');
  for (const hit of hits) console.error(hit);
  process.exit(1);
}
console.log('check:architecture OK — dependency lock, composition root, slice grammar all hold');
