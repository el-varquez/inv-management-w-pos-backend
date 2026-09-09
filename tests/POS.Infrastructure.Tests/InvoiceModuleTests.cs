using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using POS.Application.Common.Interfaces;
using POS.Application.Invoices.Commands.CreateInvoice;
using POS.Application.Invoices.Commands.VoidInvoice;
using POS.Application.Invoices.EventHandlers;
using POS.Application.Invoices.Queries.GetInvoiceById;
using POS.Application.Invoices.Queries.GetInvoices;
using POS.Domain.Entities;
using POS.Domain.Enums;
using POS.Domain.Events;
using POS.Domain.Exceptions;
using POS.Domain.Interfaces;
using POS.Infrastructure.Persistence;
using POS.Infrastructure.Persistence.Repositories;
using POS.Infrastructure.Services;
using POS.Infrastructure.Tests.Fakes;
using Xunit;

namespace POS.Infrastructure.Tests;

public class InvoiceModuleTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly AppDbContext _ctx;
    private readonly ItemRepository _items;
    private readonly CompositeItemRepository _composites;
    private readonly InvoiceRepository _invoices;
    private readonly UtangRepository _utang;
    private readonly ShiftRepository _shifts;
    private readonly StoreSettingsRepository _settings;
    private readonly StockMovementRepository _movements;
    private readonly UnitOfWork _uow;
    private readonly FakeCurrentUser _user = new();
    private readonly Guid _categoryId = Guid.NewGuid();
    private readonly Shift _shift;
    private int _codeSeq;

    public InvoiceModuleTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(_connection)
            .Options;

        _ctx = new AppDbContext(options);
        _ctx.Database.EnsureCreated();
        PaymentMethodSeeder.Seed(_ctx);

        _items = new ItemRepository(_ctx);
        _composites = new CompositeItemRepository(_ctx);
        _invoices = new InvoiceRepository(_ctx);
        _utang = new UtangRepository(_ctx);
        _shifts = new ShiftRepository(_ctx);
        _settings = new StoreSettingsRepository(_ctx);
        _movements = new StockMovementRepository(_ctx);
        _uow = new UnitOfWork(_ctx);

        _ctx.Categories.Add(new Category { Id = _categoryId, Name = "General" });
        _shift = new Shift
        {
            Number = 1,
            Status = ShiftStatus.Open,
            StartingCash = 1000m,
            OpenedAt = DateTime.UtcNow,
            OpenedBy = _user.Id,
            BusinessDay = new BusinessDay
            {
                Number = 1,
                Status = DayStatus.Open,
                OpenedAt = DateTime.UtcNow,
                OpenedBy = _user.Id
            }
        };
        _ctx.Shifts.Add(_shift);
        _ctx.StoreSettings.Add(new StoreSettings
        {
            StoreName = "Test Store",
            DefaultUtangMarkup = 1m,
            AcceptUtang = true
        });
        _ctx.SaveChanges();
    }

    private CreateInvoiceCommandHandler CreateHandler(IInvoiceNumberGenerator? numbers = null)
        => new(_items, _composites, _invoices, _utang, _shifts, _settings,
            numbers ?? new FakeInvoiceNumberGenerator(), _uow, _user);

    private VoidInvoiceCommandHandler VoidHandler()
        => new(_invoices, _settings, _uow, _user);

    private async Task<Suki> SeedSukiAsync(string name = "Aling Rosa")
    {
        var suki = new Suki { Name = name, CreatedBy = _user.Id };
        await _utang.AddSukiAsync(suki);
        await _uow.SaveChangesAsync();
        return suki;
    }

    private async Task<Item> SeedItemAsync(
        string name = "Coke Mismo 300ml", decimal price = 4m, int stock = 50,
        decimal? markup = null, bool isComposite = false)
    {
        var item = new Item
        {
            Name = name,
            ItemCode = $"I{++_codeSeq:D4}",
            CostPrice = 2m,
            SellingPrice = price,
            UtangMarkup = markup,
            Stock = stock,
            CategoryId = _categoryId,
            IsComposite = isComposite
        };
        await _items.AddAsync(item);
        await _uow.SaveChangesAsync();
        return item;
    }

    private static CreateInvoiceCommand InvoiceOf(Suki suki, Item item, int qty, decimal discount = 0m)
        => new(new List<InvoiceLineInput> { new(item.Id, qty, 0m) }, discount, suki.Id);

    private async Task SetAcceptUtangAsync(bool on)
    {
        var settings = await _ctx.StoreSettings.SingleAsync();
        settings.AcceptUtang = on;
        await _ctx.SaveChangesAsync();
    }

    private async Task CloseShiftAsync()
    {
        _shift.Status = ShiftStatus.Closed;
        _shift.ClosedAt = DateTime.UtcNow;
        await _ctx.SaveChangesAsync();
    }

    [Fact]
    public async Task An_invoice_reprices_lines_at_utang_price_and_charges_the_full_total()
    {
        var suki = await SeedSukiAsync();
        var item = await SeedItemAsync(price: 4m);

        var result = await CreateHandler().Handle(InvoiceOf(suki, item, 3), default);

        Assert.Equal("INV-TEST-0001", result.InvoiceNumber);
        Assert.Equal(15m, result.Subtotal);
        Assert.Equal(3m, result.MarkupTotal);
        Assert.Equal(15m, result.Total);
        Assert.Equal(15m, result.NewBalance);
        var line = await _ctx.InvoiceItems.SingleAsync();
        Assert.Equal(5m, line.UnitPrice);
        Assert.Equal(2m, line.CostPrice);
        var invoice = await _ctx.Invoices.SingleAsync();
        Assert.Equal(_shift.Id, invoice.ShiftId);
        Assert.Equal(suki.Id, invoice.SukiId);
    }

    [Fact]
    public async Task Item_markup_overrides_the_default_and_zero_opts_out()
    {
        var suki = await SeedSukiAsync();
        var custom = await SeedItemAsync("Custom", price: 10m, markup: 2.5m);
        var free = await SeedItemAsync("Free", price: 10m, markup: 0m);

        var command = new CreateInvoiceCommand(
            new List<InvoiceLineInput> { new(custom.Id, 1, 0m), new(free.Id, 1, 0m) }, 0m, suki.Id);
        var result = await CreateHandler().Handle(command, default);

        Assert.Equal(22.5m, result.Total);
        Assert.Equal(2.5m, result.MarkupTotal);
    }

    [Fact]
    public async Task An_invoice_writes_no_payment_no_sale_and_no_drawer_movement()
    {
        var suki = await SeedSukiAsync();
        var item = await SeedItemAsync();

        await CreateHandler().Handle(InvoiceOf(suki, item, 2), default);

        Assert.Equal(0, await _ctx.Payments.CountAsync());
        Assert.Equal(0, await _ctx.Sales.CountAsync());
        Assert.Equal(0, await _ctx.CashDrawerMovements.CountAsync());
    }

    [Fact]
    public async Task An_invoice_needs_an_open_shift()
    {
        var suki = await SeedSukiAsync();
        var item = await SeedItemAsync();
        await CloseShiftAsync();

        var ex = await Assert.ThrowsAsync<DomainException>(
            () => CreateHandler().Handle(InvoiceOf(suki, item, 1), default));

        Assert.Equal("No open shift — declare starting cash to start selling.", ex.Message);
    }

    [Fact]
    public async Task An_invoice_is_refused_while_utang_is_off()
    {
        var suki = await SeedSukiAsync();
        var item = await SeedItemAsync();
        await SetAcceptUtangAsync(false);

        var ex = await Assert.ThrowsAsync<DomainException>(
            () => CreateHandler().Handle(InvoiceOf(suki, item, 1), default));

        Assert.Equal("Utang is turned off — turn it on in web admin Settings.", ex.Message);
    }

    [Fact]
    public async Task An_unknown_suki_is_refused()
    {
        var item = await SeedItemAsync();
        var command = new CreateInvoiceCommand(
            new List<InvoiceLineInput> { new(item.Id, 1, 0m) }, 0m, Guid.NewGuid());

        await Assert.ThrowsAsync<NotFoundException>(
            () => CreateHandler().Handle(command, default));
    }

    [Fact]
    public void An_empty_suki_is_refused_by_the_validator()
    {
        var result = new CreateInvoiceCommandValidator().Validate(new CreateInvoiceCommand(
            new List<InvoiceLineInput> { new(Guid.NewGuid(), 1, 0m) }, 0m, Guid.Empty));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.ErrorMessage == "Pick a suki to charge.");
    }

    [Fact]
    public async Task Insufficient_stock_is_refused()
    {
        var suki = await SeedSukiAsync();
        var item = await SeedItemAsync(stock: 1);

        await Assert.ThrowsAsync<InsufficientStockException>(
            () => CreateHandler().Handle(InvoiceOf(suki, item, 2), default));
    }

    [Fact]
    public async Task A_discount_larger_than_the_subtotal_is_refused()
    {
        var suki = await SeedSukiAsync();
        var item = await SeedItemAsync(price: 4m);

        var ex = await Assert.ThrowsAsync<DomainException>(
            () => CreateHandler().Handle(InvoiceOf(suki, item, 1, discount: 6m), default));

        Assert.Equal("Total cannot be negative after discounts.", ex.Message);
    }

    [Fact]
    public async Task Invoice_numbers_continue_from_the_days_max_suffix()
    {
        var suki = await SeedSukiAsync();
        var item = await SeedItemAsync();
        _ctx.Invoices.Add(new Invoice
        {
            InvoiceNumber = $"INV-{DateTime.Now:yyyyMMdd}-0007",
            SukiId = suki.Id,
            ShiftId = _shift.Id,
            CreatedBy = _user.Id
        });
        await _ctx.SaveChangesAsync();

        var result = await CreateHandler(new InvoiceNumberGenerator(_invoices))
            .Handle(InvoiceOf(suki, item, 1), default);

        Assert.Equal($"INV-{DateTime.Now:yyyyMMdd}-0008", result.InvoiceNumber);
    }

    [Fact]
    public async Task An_invoice_number_collision_is_retried()
    {
        var suki = await SeedSukiAsync();
        var item = await SeedItemAsync();
        _ctx.Invoices.Add(new Invoice
        {
            InvoiceNumber = "INV-TEST-0001",
            SukiId = suki.Id,
            ShiftId = _shift.Id,
            CreatedBy = _user.Id
        });
        await _ctx.SaveChangesAsync();

        var result = await CreateHandler(
                new FakeCollidingInvoiceNumberGenerator("INV-TEST-0001", "INV-TEST-0002"))
            .Handle(InvoiceOf(suki, item, 1), default);

        Assert.Equal("INV-TEST-0002", result.InvoiceNumber);
    }

    [Fact]
    public async Task The_created_event_deducts_stock_including_composite_expansion()
    {
        var component = await SeedItemAsync("Sachet", stock: 50);
        var bundle = await SeedItemAsync("Bundle 3s", stock: 0, isComposite: true);
        _ctx.CompositeItems.Add(new CompositeItem
        {
            ParentItemId = bundle.Id, ComponentItemId = component.Id, Quantity = 3m
        });
        await _ctx.SaveChangesAsync();

        var handler = new InvoiceCreatedEventHandler(_items, _composites, _movements);
        await handler.Handle(new InvoiceCreatedEvent(
            Guid.NewGuid(), new List<(Guid, int)> { (bundle.Id, 2) }, _user.Id), default);
        await _ctx.SaveChangesAsync();

        Assert.Equal(44, (await _items.GetByIdAsync(component.Id))!.Stock);
        var movement = await _ctx.StockMovements.SingleAsync();
        Assert.Equal(StockMovementType.Sale, movement.Type);
        Assert.Equal(-6, movement.Quantity);
    }

    [Fact]
    public async Task Voiding_flags_the_invoice_and_raises_the_restock_event()
    {
        var suki = await SeedSukiAsync();
        var item = await SeedItemAsync();
        var created = await CreateHandler().Handle(InvoiceOf(suki, item, 2), default);

        await VoidHandler().Handle(new VoidInvoiceCommand(created.InvoiceId), default);

        var invoice = await _invoices.GetByIdAsync(created.InvoiceId);
        Assert.True(invoice!.IsVoided);
        Assert.Equal(_user.Id, invoice.VoidedBy);
        Assert.NotNull(invoice.VoidedAt);
        Assert.Contains(invoice.DomainEvents, e => e is InvoiceVoidedEvent);
        Assert.Equal(0m, await _utang.GetBalanceAsync(suki.Id));
    }

    [Fact]
    public async Task The_voided_event_restores_stock()
    {
        var item = await SeedItemAsync(stock: 48);

        var handler = new InvoiceVoidedEventHandler(_items, _composites, _movements);
        await handler.Handle(new InvoiceVoidedEvent(
            Guid.NewGuid(), new List<(Guid, int)> { (item.Id, 2) }, _user.Id), default);
        await _ctx.SaveChangesAsync();

        Assert.Equal(50, (await _items.GetByIdAsync(item.Id))!.Stock);
        Assert.Equal(StockMovementType.Return, (await _ctx.StockMovements.SingleAsync()).Type);
    }

    [Fact]
    public async Task Voiding_needs_no_open_shift()
    {
        var suki = await SeedSukiAsync();
        var item = await SeedItemAsync();
        var created = await CreateHandler().Handle(InvoiceOf(suki, item, 1), default);
        await CloseShiftAsync();

        await VoidHandler().Handle(new VoidInvoiceCommand(created.InvoiceId), default);

        Assert.True((await _ctx.Invoices.SingleAsync()).IsVoided);
    }

    [Fact]
    public async Task Voiding_twice_is_refused()
    {
        var suki = await SeedSukiAsync();
        var item = await SeedItemAsync();
        var created = await CreateHandler().Handle(InvoiceOf(suki, item, 1), default);
        await VoidHandler().Handle(new VoidInvoiceCommand(created.InvoiceId), default);

        var ex = await Assert.ThrowsAsync<DomainException>(
            () => VoidHandler().Handle(new VoidInvoiceCommand(created.InvoiceId), default));

        Assert.Equal("This invoice is already voided.", ex.Message);
    }

    [Fact]
    public async Task Voiding_is_refused_while_utang_is_off()
    {
        var suki = await SeedSukiAsync();
        var item = await SeedItemAsync();
        var created = await CreateHandler().Handle(InvoiceOf(suki, item, 1), default);
        await SetAcceptUtangAsync(false);

        var ex = await Assert.ThrowsAsync<DomainException>(
            () => VoidHandler().Handle(new VoidInvoiceCommand(created.InvoiceId), default));

        Assert.Equal("Utang is turned off — turn it on in web admin Settings.", ex.Message);
    }

    [Fact]
    public async Task Voiding_leaves_payments_alone_and_may_leave_credit()
    {
        var suki = await SeedSukiAsync();
        var item = await SeedItemAsync(price: 4m);
        var created = await CreateHandler().Handle(InvoiceOf(suki, item, 3), default);
        _ctx.Payments.Add(new Payment { SukiId = suki.Id, Amount = 10m, CreatedBy = _user.Id });
        await _ctx.SaveChangesAsync();

        await VoidHandler().Handle(new VoidInvoiceCommand(created.InvoiceId), default);

        Assert.False((await _ctx.Payments.SingleAsync()).IsVoided);
        Assert.Equal(-10m, await _utang.GetBalanceAsync(suki.Id));
    }

    [Fact]
    public async Task The_list_and_detail_carry_the_suki_name_and_lines()
    {
        var suki = await SeedSukiAsync("Mang Ben");
        var item = await SeedItemAsync("Bread", price: 9m);
        var created = await CreateHandler().Handle(InvoiceOf(suki, item, 2), default);

        var page = await new GetInvoicesQueryHandler(_invoices)
            .Handle(new GetInvoicesQuery(null, null, 1, 20), default);
        var detail = await new GetInvoiceByIdQueryHandler(_invoices)
            .Handle(new GetInvoiceByIdQuery(created.InvoiceId), default);

        var row = Assert.Single(page.Items);
        Assert.Equal("Mang Ben", row.SukiName);
        Assert.Equal(20m, row.Total);
        Assert.Equal("Mang Ben", detail.SukiName);
        Assert.Equal(_shift.Id, detail.ShiftId);
        var line = Assert.Single(detail.Lines);
        Assert.Equal("Bread", line.ItemName);
        Assert.Equal(10m, line.UnitPrice);
    }

    [Fact]
    public async Task New_balance_accumulates_across_invoices()
    {
        var suki = await SeedSukiAsync();
        var item = await SeedItemAsync(price: 4m);

        await CreateHandler().Handle(InvoiceOf(suki, item, 3), default);
        var second = await CreateHandler().Handle(InvoiceOf(suki, item, 1), default);

        Assert.Equal(20m, second.NewBalance);
    }

    public void Dispose()
    {
        _ctx.Dispose();
        _connection.Dispose();
    }
}
