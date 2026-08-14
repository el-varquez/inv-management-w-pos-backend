#!/usr/bin/env node
/**
 * Conventions guard: branch names are <type>/<kebab>, commit subjects follow
 * Conventional Commits v1.0.0 (subject <= 100 chars; GitHub-generated
 * `Revert "..."` commits allowed; merge commits never checked).
 * CI (PR-only) sets BRANCH_NAME and COMMIT_RANGE; locally the defaults are
 * the current branch and origin/main..HEAD. --branch <name> / --subject <text>
 * validate one input and exit — used by mutation tests and pre-commit checks.
 */
import { execSync } from 'node:child_process';

const TYPES = ['feat', 'fix', 'chore', 'docs', 'refactor', 'perf', 'test', 'build', 'ci', 'style', 'revert'];
const BRANCH_RE = new RegExp(`^(${TYPES.join('|')})/[a-z0-9]+(-[a-z0-9]+)*$`);
const SUBJECT_RE = new RegExp(`^(${TYPES.join('|')})(\\([a-z0-9-]+\\))?!?: .+$`);
const MAX_SUBJECT = 100;

const branchProblem = (name) =>
  BRANCH_RE.test(name) ? null : `branch "${name}" — expected <type>/<kebab-name> with type in ${TYPES.join('|')}`;

const subjectProblem = (subject) => {
  if (/^Revert ".+"$/.test(subject)) return null;
  if (!SUBJECT_RE.test(subject)) {
    return `commit "${subject}" — expected <type>(scope)?!?: description (Conventional Commits v1.0.0)`;
  }
  if (subject.length > MAX_SUBJECT) {
    return `commit "${subject.slice(0, 40)}…" — subject is ${subject.length} chars, max ${MAX_SUBJECT}`;
  }
  return null;
};

const git = (cmd) => execSync(cmd, { encoding: 'utf8' }).trim();

const args = process.argv.slice(2);
const flag = (name) => {
  const i = args.indexOf(name);
  return i >= 0 && i + 1 < args.length ? args[i + 1] : null;
};

const problems = [];
const onlyBranch = flag('--branch');
const onlySubject = flag('--subject');

if (onlyBranch !== null || onlySubject !== null) {
  if (onlyBranch !== null) {
    const p = branchProblem(onlyBranch);
    if (p) problems.push(p);
  }
  if (onlySubject !== null) {
    const p = subjectProblem(onlySubject);
    if (p) problems.push(p);
  }
} else {
  const branch = process.env.BRANCH_NAME || git('git rev-parse --abbrev-ref HEAD');
  if (branch !== 'main') { // main is not a work branch; CI only ever checks PR head refs
    const p = branchProblem(branch);
    if (p) problems.push(p);
  }

  const range = process.env.COMMIT_RANGE || 'origin/main..HEAD';
  const log = git(`git log --no-merges --format=%s ${range}`);
  for (const subject of log.split(/\r?\n/).filter(Boolean)) {
    const p = subjectProblem(subject);
    if (p) problems.push(p);
  }
}

if (problems.length > 0) {
  console.error('check:conventions FAILED — Conventional Commits v1.0.0 + <type>/<kebab> branch names\n');
  for (const problem of problems) console.error(problem);
  process.exit(1);
}
console.log('check:conventions OK');
