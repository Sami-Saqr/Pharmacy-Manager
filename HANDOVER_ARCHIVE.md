# CarPartManager - Handover Document

## Latest: Excel export Part B — branch `feature-excel-export`

**Status:** Reused the Part A ClosedXML service for customer list and customer-statement workbooks, filtered expenses with active-only totals and category percentages, all accounting-report datasets, and stocktake session/blank counting sheets. Stocktake export supports any selected history session; blank sheets hide book quantities by default and ask before showing them. No package or database changes were made.

**Implementation:** Screen ViewModels query their existing services with the current filters/period and use the shared asynchronous export/save flow. Workbook mapping is centralized in `Services/PartBExcelExportMappers.cs`; the shared writer handles safe text, sheet-name sanitation, formulas, summaries, highlights, confidential report footers, and print setup. To add another export, add a mapper using `ExcelExportSheet`/`ExcelExportColumn`, fetch filtered source data from the existing service, then call `ExportWorkbookAsync` from the ViewModel and `ExcelExportDialog` from the view.

**Files:** Updated shared export definitions/writer, Customers/Expenses/Reports/Inventory ViewModels and views, and added `CarPartManager.Tests/PartBExcelExportMapperTests.cs`. ClosedXML **0.105.1** remains the only added package.

**Validation:** `dotnet build CarPartManager.csproj --no-restore` succeeded with 0 errors and 8 existing CS8602 warnings in `ViewModels/InventoryViewModel.cs`. The full existing test project passed **23/23** tests, including workbook checks for customer text safety, statement balances/summary, active-only expense formulas and category shares, report formulas/warnings, sheet names, stocktake formulas, blank counted cells, and book-quantity visibility. The app was not run and no database files were accessed.

**Remaining/manual checks:** Open customer list/statements, expenses, every reports workbook tab, and stocktake session/blank-sheet exports in Excel; verify Arabic RTL rendering, filtering/formula results, and A4 print previews. Customer-list last-payment date remains omitted because the list model/service does not expose it; the field is optional. Products export remains a separate follow-up because it uses a distinct list/mapping.

## Latest: Excel export Part A — branch `feature-excel-export`

**Status:** Added reusable `.xlsx` export infrastructure and export buttons for Inventory stock overview, Inventory movements, Sales invoice history, and Purchase invoice history. The three history/inventory exports use their current search, stock/status, movement date/product/type filters and include unpaged database results. Products has a separate list/mapping, so its export button is deferred rather than duplicating the inventory mapper.

**Implementation:** Added ClosedXML **0.105.1** (the only new package). `IExcelExportService` and `ExcelExportService` create RTL, styled workbooks with filters, frozen headers, typed cells, subtotals, highlights, print setup, safe text handling, and temp-file replacement. Screen mappers live in `Services/ExcelExportMappers.cs`; invoice history export queries are in the SQLite invoice services; view models expose exporting/empty-data state; shared SaveFileDialog and success/open handling is in `Views/ExcelExportDialog.cs`. Part B extends this shared flow through `Services/PartBExcelExportMappers.cs`.

**Files:** Added `Services/ExcelExportDefinition.cs`, `ExcelExportMappers.cs`, `ExcelExportService.cs`, `IExcelExportService.cs`, `InvoiceExportRecord.cs`, `ViewModels/ExcelExportViewModelBase.cs`, `Views/ExcelExportDialog.cs`, and `CarPartManager.Tests/ExcelExportServiceTests.cs`. Updated `CarPartManager.csproj`, inventory/invoice service contracts and implementations, the three export ViewModels, and the Inventory/Sales/Purchase history views and code-behind.

**Validation:** ClosedXML restore succeeded. `dotnet build CarPartManager.csproj --no-restore` succeeded with 0 errors and 8 existing CS8602 warnings in `ViewModels/InventoryViewModel.cs`. The targeted Excel export tests passed (2/2): workbook RTL, headers, typed codes/numbers/dates, injection strings, subtotal formulas, and locked-target preservation. The app was not run and no database files were accessed.

**Remaining/manual checks:** Open stock, movement, sales-history, and purchase-history exports in Excel; check Arabic/RTL layout, currency/date readability, print titles/page footer and filter-aware totals. Compare invoice and stock totals with their screens under searches/filters. Part B manual checks are listed in the latest section above.

## Latest: Home tile navigation — branch `feature-home-tiles`

**Home tile visual update (2026-10-28):** Filled the three Home tile groups with high-contrast green, blue, and indigo palette brushes; added group-specific darker hover colors, near-white text/icons, and a visible keyboard focus ring. Increased tile titles/descriptions and section heading sizes, emphasized the POS title, and raised tile dimensions/minimums with two-line wrapped descriptions. Home visuals were not runtime-tested; verify tile reflow and text/focus rendering at 1340×820 and the minimum 1040×680.

**Status:** Replaced the right-side navigation sidebar with a Home screen of adaptive, grouped Arabic navigation tiles and a shared full-width inner-screen header. The Home screen is the startup destination; its footer carries the existing store name and version. POS is visually emphasized. The header provides the current screen title, a “← الرئيسية” button, existing user/date/branch/database details, and Alt+Home navigation.

**Unsaved POS work:** `SalesInvoicesViewModel` exposes the `IHasUnsavedChanges` contract when a nonempty invoice is neither posted nor cancelled. Returning Home by button or Alt+Home prompts in Arabic; choosing “الرجوع للشاشة” stays on POS, while “الخروج وتجاهل التغييرات” returns Home. Home tile navigation is not guarded. Navigation ignores repeated tile requests during screen loading and preserves deferred async loads.

**Files:** Added `Models/HomeTile.cs`, `ViewModels/HomeViewModel.cs`, `Views/HomeView.xaml` and code-behind, `MVVM/IHasUnsavedChanges.cs`, and the unsaved-changes dialog view/code-behind. Updated `App.xaml`, `MainWindow.xaml`, `ViewModels/MainViewModel.cs`, `ViewModels/SalesInvoicesViewModel.cs`, `Models/NavItem.cs`, and the Home/control color styles. Removed verified sidebar-only styles/colors and the unused navigation selection state. The earlier `a62d36e` commit contains the Home tile data/view setup; the shell, guard, and cleanup are included in the current changes.

**Validation and remaining work:** `dotnet build CarPartManager.csproj --no-restore` succeeded with 0 errors and 8 existing CS8602 warnings in `ViewModels/InventoryViewModel.cs`. The app was not run and visual/keyboard/unsaved-dialog behavior remains for manual verification at 1340×820 and 1040×680. Verify Home reflow/scroll, all 12 destinations, full-width inner screens, Alt+Home, button navigation, and both POS prompt choices with an unsaved invoice. No database or schema files were changed. Static inspection indicates Reports synchronously reads completed task results and Returns synchronously dispatches collection updates; these screen-load paths were not refactored. Other responsive loading remains as before.

## Latest: Navigation and cashier responsiveness implementation — branch `fix-performance`
**Date:** 2026-10-28

**Changes:** `MainViewModel` now assigns the destination ViewModel before queuing its first/refresh load at background dispatcher priority. Removed constructor-started loads from the navigated screens to avoid duplicate startup queries. Added a non-blocking Arabic loading indicator in `MainWindow.xaml`, guarded duplicate in-flight loads per ViewModel, ignored superseded queued navigations, and queued a refresh when returning to a screen whose earlier load is still active. `App.OnStartup` initialization remains one-time; per-operation DbContexts remain unchanged.

**Cashier and dashboard:** Added a header-only invoice-summary query and used it for POS/history lists; `GetAllInvoicesAsync` remains available with invoice items for callers that need the full graph. POS invoice posting now fetches all relevant tracked products in one query before validating and updating stock, while preserving per-line checks and movements. Dashboard totals/counts are aggregated in SQLite, low-stock details are projected, and only the newest seven rows per source are read to construct the same seven-item recent-activity list. Customer-suggestion failures now set an Arabic status and are reported to the user rather than silently swallowed.

**Purchase entry:** Replaced the page-level `ScrollViewer`/`StackPanel` with bounded `Grid` rows; the invoice-items `DataGrid` occupies a star-sized row and keeps internal automatic scrollbars with recycling virtualization enabled. Other DataGrid screens were not changed because no matching virtualization defect was identified in the diagnosis.

**Navigation limitation:** Already-running database calls are not cancellable because the service methods do not accept cancellation tokens. Superseded queued loads are skipped; a screen is not loaded concurrently with itself, and returning during a load queues a fresh read. Runtime timing and visual behavior were not measured because the app was not run.

**Validation:** Main-project builds succeeded with 0 errors and the same 8 pre-existing nullable warnings in `ViewModels/InventoryViewModel.cs:218-221`. The test project compiled successfully with 0 warnings and 0 errors; tests were not run to avoid touching database files. No `.db` files were accessed, no schema/packages/business rules changed, and no timing instrumentation was added.

**Manual verification required:** At 1340×820 and the minimum 1040×680, click Dashboard, Inventory, Products, POS, Sales History, Purchase Entry, and Reports; confirm the destination shell appears before the loading indicator, rapid navigation does not leave a stale screen, and re-entry refreshes data. In POS, test customer suggestions, barcode add/re-scan, saving/posting an invoice, stock freshness after returning to Inventory, and history filtering. Compare Dashboard totals/recent activity with the corresponding records. In Purchase Entry, resize while viewing line items and confirm columns remain separated and the grid scrolls internally. Measure click-to-shell time manually; the under-150 ms target is not verified here.

## Latest: Navigation and cashier performance diagnosis — branch `fix-performance`
**Date:** 2026-10-28

**Scope:** Static inspection only, as requested before approval. No app run, database access, code instrumentation, behavior change, schema change, or build. No `AGENTS.md` exists in the repository. No timings were collected; savings below are hypotheses to validate, not measured promises.

**Ranked likely causes:**
1. **Eager, duplicated screen loads (highest confidence):** `ViewModels/MainViewModel.cs:57-80` constructs every screen ViewModel at once. Several constructors immediately start database loads (`ProductsViewModel.cs:96`, `InventoryViewModel.cs:226`, `DashboardViewModel.cs:182`, `SalesInvoicesViewModel.cs:492-529`, `PurchaseInvoicesViewModel.cs:203-206`, `ReturnsViewModel.cs:250`, `CustomersViewModel.cs:324`, `ReportsViewModel.cs:64`). `MainViewModel.cs:127-204` then starts another load on each navigation, including first entry. Load guards vary; there is no common coalescing/cancellation policy. This creates simultaneous SQLite work at startup and redundant queries on entry. Estimated benefit: potentially a substantial reduction in cold-start database/UI contention; destination assignment itself is already after a non-blocking async call, so the exact click-to-shell gain cannot be inferred from code.
2. **POS loads catalog plus the complete invoice archive:** `SalesInvoicesViewModel.cs:545-558` reads every product and calls `GetAllInvoicesAsync`; `Services/SqliteSalesInvoiceService.cs:45-55` includes all invoice items. The ViewModel is cached but navigation calls `LoadAllDataAsync` again (`MainViewModel.cs:162`). Initial setup also calls `PrepareNewInvoiceAsync` (`SalesInvoicesViewModel.cs:524-529`), which generates a number by reading every invoice number for the current year (`SqliteSalesInvoiceService.cs:18-39`). Cost grows with archive size. Estimated benefit: bounding or projecting history and avoiding redundant fetches should reduce POS work in proportion to excluded invoice/item rows; no safe millisecond estimate without measurement.
3. **Per-line database reads when posting invoices:** `Services/SqliteSalesInvoiceService.cs:328-330` queries the product once for every invoice line while posting. This is N product reads for N lines; batching product reads can remove up to N−1 read round-trips per invoice. This affects save/post responsiveness, not the initial menu click.
4. **Dashboard reads whole tables and aggregates in memory:** `Services/SqliteDashboardService.cs:23-107` materializes products, all purchase/sales invoices, and all returns before filtering and summarizing. Work and allocations grow with business history. Database-side aggregate/projection queries can change this from processing all rows in the application to returning summary/visible rows; gain depends on database size.
5. **Invoice-list reads are unbounded:** `SqliteSalesInvoiceService.cs:45-55` and `SqlitePurchaseInvoiceService.cs:44-55` return complete histories with item collections; their history screens load on each navigation (`MainViewModel.cs:148-169`). Paging/summary projections would reduce load and UI collection construction proportional to rows removed, subject to approval of the history UX.
6. **Virtualization risk on purchase entry:** `Views/PurchaseInvoicesView.xaml:12-13` wraps the page in a `ScrollViewer` and `StackPanel`, with its items `DataGrid` at line 193. The outer scroll host can disable effective DataGrid virtualization and forces unbounded measurement. The draft's usual small line count limits impact, but it is a direct layout/performance issue. Other observed DataGrids use default virtualization or explicit recycling; no disabled virtualization setting was found.

**Other checks:** `MainViewModel.NavigateTo` starts async loading and assigns `CurrentViewModel` immediately (`ViewModels/MainViewModel.cs:117-205`); no `.Result`/`.Wait()` was found on the normal navigation path. The `.GetAwaiter().GetResult()` uses found in `App.xaml.cs` are in `RunSelfDiagnostics`, guarded by `--run-tests`. Services generally create/dispose a `CarPartsDbContext` per operation and use `AsNoTracking` for reads; no shared long-lived context was found. `DbInitializer.InitializeAsync` runs once in `App.OnStartup` (`App.xaml.cs:16-35`), not per screen/context; it performs `EnsureCreated`, many idempotent DDL/upgrade checks, and seeding checks (`Data/DbInitializer.cs:16-34, 37-289`), so it is a startup-only candidate, not a navigation cause. Reports starts nine report requests together (`ReportsViewModel.cs:370-392`). No blur/animation/image-heavy navigation effects were found; shared card shadows are low-opacity.

**Error-path note:** `SalesInvoicesViewModel.cs:1242` has an unreported catch in customer suggestion loading. Cancellation is handled separately, but other async failures are currently swallowed; any approved async refactor must keep failures visible to the user in Arabic.

**Proposed work after approval (in requested order):** (1) make navigation return/render the destination shell first, then coordinate background loads with cancellation/stale-result guards and a light loading state; prevent duplicate in-flight loads and double-click work. (2) keep startup initialization one-time (already startup-only); first measure startup cost before relocating it. (3) retain per-operation contexts; optimize POS/dashboard/history reads with projections, database-side filtering/aggregation, and batch product reads during invoice posting. (4) preserve freshness on re-entry while avoiding needless duplicate loads; cache is already in place. (5) bound the purchase-entry grid and verify virtualization; consider history paging/debounced searches only where approved. (6) add indexes only with query-plan evidence. No schema or business-rule change is proposed.

**Instrumentation:** none added. Next diagnostic measurement, if approved, should use temporary `Stopwatch` + `Debug.WriteLine` spans around `NavigateTo`, screen load entry/completion, POS barcode lookup, and invoice post, then be removed. App must remain unrun unless you separately authorize it.

## Latest: Responsive DataGrid column sizing ? branch `fix-layout`
**Date:** 2026-10-28

**Changes:** `Views/InventoryView.xaml` now gives long-text columns in the stock overview, movement log, and stocktake lines flexible star sizing with minimum widths. The stock name receives more room in the overview; numeric/date columns remain fixed, and the grids retain automatic horizontal scrolling and the existing trimming/tooltips.

**Review:** Sales and purchase invoice-history grids already use bounded `Grid` rows, automatic horizontal scrolling, minimum widths for every column, flexible customer/supplier/notes columns, and ellipsis/tooltips for long values; no additional XAML change was warranted there. Products, customers, expenses, reports, and POS item grids were also reviewed; no matching unbounded-grid or column-sizing defect was found, so they were left unchanged.

**Build:** `dotnet build CarPartManager.csproj` ? **Build succeeded, 0 Error(s), 8 Warning(s)**. The warnings are existing `CS8602` nullable dereference warnings in `ViewModels/InventoryViewModel.cs:218?221`. The app was not run and no `.db` file was touched.

**Manual verification required:** At the default window size (1340?820) and the minimum supported size (1040?680), resize through the inventory overview, movements, and stocktake grids, plus sales and purchase invoice history. Confirm column boundaries stay distinct, long text trims with a tooltip, and horizontal scrolling appears where the minimum column widths exceed the viewport. Also spot-check products, customers, expenses, reports, and POS item grids at the minimum size.

## Latest: Stocktake tab did nothing after creating a session — FIXED (branch `fix-stocktake`)
**Date:** 2026-10-27
**Symptom:** "الجرد" tab: a session could be created and opened, but counting/adding a product did nothing — no row appeared and no message was shown. Reproduced on a fresh database.

**Root causes (confirmed, code-only — no `.db` touched):**
1. `Views/InventoryView.xaml` (~L498 and L527): the count-lines `DataGrid` (`Grid.Row="2"`) and the sessions-history card were **both in `Grid.Row="2"`**. The history card is declared last and uses `ContentPanelCardStyle` (opaque `CardBackgroundBrush` + shadow), so it painted **on top of** every count row. The lines existed (snapshot generated correctly by `SqliteStocktakeService.CreateSessionAsync`) and any recorded count was persisted — it was simply invisible.
2. `ViewModels/InventoryViewModel.cs:221` — `CountProductCommand` was created but **bound nowhere in XAML**, and `ExecuteCountProductAsync` started with a silent `if (SelectedStocktakeLine == null) return;`, so the click/selection path produced no row and no message.
3. `Views/InventoryView.xaml` (~L470) — the barcode box had **no Enter handling**, so a USB scanner's Enter was a no-op; and `ExecuteCountByBarcodeAsync` returned **silently** when the search text was empty.
4. `Services/SqliteStocktakeService.GetLineByBarcodeAsync` (L297–303) matched the barcode **exactly** only — no name / part-number lookup, no Arabic-digit normalisation.

Ruled out: `DbInitializer.EnsureStocktakeTablesCreatedAsync` DDL matches the EF model (`StocktakeSessions` / `StocktakeLines`, `SessionId NOT NULL`, FK cascade); `CreateSessionAsync` does generate one line per product; the count-time `BookQuantity` refresh (`CountProductAsync`) has no fresh-DB issue.

**Changes (2 commits):**
1. `cecf9de` — `Views/InventoryView.xaml`: history card moved to `Grid.Row="4"`, so the count grid is visible again.
2. `069e4fd` — counting actually works:
   - `Views/InventoryView.xaml` + `InventoryView.xaml.cs`: the barcode box got `x:Name="StocktakeBarcodeTextBox"` and a `PreviewKeyDown` handler that commits the binding (`UpdateSource())`), executes `CountByBarcodeCommand` and keeps focus (and select-all) in the field — same scanner pattern as POS (`SalesInvoicesView`).
   - `ViewModels/InventoryViewModel.cs`: the counted quantity is now a **text** input (`StocktakeCountQuantityText`, `PhoneHelper.NormalizeDigits`, integer ≥ 0) so Arabic-Indic digits are accepted; a per-keystroke filter over the loaded lines (name / part number / barcode) drives the grid; `FindStocktakeLine` resolves barcode → part number → unique name (ambiguous name match is refused instead of counting the wrong product); every silent `return` was replaced by an explicit Arabic `MessageBox` (no active session, empty term, empty/invalid quantity, "not found" vs "outside the session scope", ambiguous match, status guard `لا يمكن العد في جلسة معتمدة أو ملغاة.`); added a visible hint + `StatusMessage` line in the counting card.
   - `Views/InventoryView.xaml`: new "تسجيل الصنف المحدد" button bound to the previously-unbound `CountProductCommand` (click path: select a row → enter quantity → click).
**Unchanged:** approval rules (review-only, once, `StockMovements` in one transaction, stock untouched before approval), the DDL, and the service/interface.

**Build/Test:** `dotnet build CarPartManager.csproj` → **Build succeeded, 0 Error(s)** (8 pre-existing `CS8602` warnings in `InventoryViewModel.cs` 218–221); `dotnet test CarPartManager.Tests\CarPartManager.Tests.csproj` → **Passed! Failed: 0, Passed: 15**, EXIT=0. App not run, no `.db` touched, no new packages.

**Manual test steps (not runtime-tested here):** fresh DB → "المخزون والجرد" → "الجرد" → "جلسة جديدة" → the lines grid must list the products. Type a quantity (Arabic digits accepted) → scan a barcode and press Enter (or type a name / part number and press Enter): the row must refresh immediately with الرصيد وقت العد / المعدود / الفرق / الحالة and the focus must stay in the field. Or select a row in the grid and press "تسجيل الصنف المحدد". An unknown code, a code outside the session scope, an ambiguous name, an empty/invalid quantity and counting on an approved/cancelled session must each show a clear Arabic message.

**Risks:** low. The fix is XAML/VM only. Known design choice: the counted quantity is edited in the counting card's quantity box (then Enter / "تسجيل الصنف المحدد"), not inline in the grid cell. `IStocktakeService.CountByBarcodeAsync` is left in place but no longer called by the VM (resolution now happens in memory against the loaded session lines).

## Latest: Unit cost at sale stored on sales lines — reports no longer drift (branch `feature-cost-at-sale`)
**Date:** 2026-10-24
**Problem:** sales invoice lines did not store the unit cost at sale time, so COGS in the accounting reports was computed from `Products.PurchasePrice` (the **latest** purchase price — updated by purchase approval at `SqlitePurchaseInvoiceService.cs:292`). Historical profit therefore changed whenever a product's cost changed.

**Findings (inspect-first, code-only, no DB touched):**
- Sales lines are created in memory by POS (`SalesInvoicesViewModel` → `new SalesInvoiceItemModel(...)`) and persisted **only** by `SqliteSalesInvoiceService`: `SaveDraftInvoiceAsync` (drafts) and `PostInvoiceAsync` (posting) — no edit/duplicate/import path; reversal paths are sales returns (reference the original line via `SalesReturnItemModel.SalesInvoiceItemId`) and invoice cancellation (flips status to `ملغاة`, excluded by the reports' `Status == "معتمدة"` filter).
- No new costing method was introduced: the cost written at sale is exactly `ProductModel.PurchasePrice`, the same field the reports already read through `GetCostPricesAsync`.

**Changes (4 commits + 1 test commit):**
1. `8979d51` — `Models/SalesInvoiceItemModel.cs`: nullable `decimal? UnitCostAtSale`; `Data/CarPartsDbContext.cs`: `HasPrecision(18, 2)`; `Data/DbInitializer.cs`: `UnitCostAtSale TEXT NULL` in the `SalesInvoiceItems` DDL **plus** an additive, idempotent upgrade — `EnsureUnitCostAtSaleColumnAsync` checks `PRAGMA table_info("SalesInvoiceItems")` and runs `ALTER TABLE ... ADD COLUMN` only when the column is missing. Nothing is dropped or recreated.
2. `3cf810a` — `Services/SqliteSalesInvoiceService.cs`: inside `PostInvoiceAsync` (after the transaction starts, before the save), the current `PurchasePrice` of each item's product is read and stamped on the new lines in **both** branches (new invoice + completing a draft). Drafts keep `NULL`. Totals, stock deduction and approval logic untouched.
3. `b6ba995` — reports now use the stored cost: `CogsBeforeReturns = Σ(Quantity × (UnitCostAtSale ?? current cost))`; returns reverse the **original line's stored cost** when linkable (`SalesInvoiceItemId`), else the same fallback; `GetItemProfitabilityAsync` uses the identical formula (cross-check against gross profit preserved); `IsCogsEstimated` is now `estimatedLineCount > 0` with new `EstimatedLineCount` / `EstimatedInvoiceCount` on `ProfitSummaryModel`/`ItemProfitabilityModel`; the Arabic note in `ReportsViewModel` states how many lines/invoices of the period are estimated (Part A `CogsNoteText`, Part B `ItemProfitNoteText`).
4. `54d2c5f` — `CarPartManager.Tests/CostAtSaleTests.cs` (6 facts, in-memory SQLite) covering (a) legacy DB without the column gets it added safely, (b) posted invoice stamps the cost / draft does not, (c) changing the product cost later does not change historical COGS, (d) legacy `NULL` lines use the fallback and set the estimate counters while clean periods do not, (e) a sellable return reverses the stored (not current) cost. `SqliteReportsService.CreateContext()` became `protected virtual` (same pattern as the other services) so the tests can point it at a test connection — no behaviour change.
- **No backfill:** old lines stay `NULL`; they are only *reported* as estimated.

**Build/Test:** `dotnet build CarPartManager.csproj` → **0 Error(s)** (8 pre-existing `CS8602` warnings in `InventoryViewModel.cs`); `dotnet test CarPartManager.Tests\CarPartManager.Tests.csproj` → **Passed! Failed: 0, Passed: 15** (9 previous + 6 new), EXIT=0. App not run, no `.db` touched.

**Manual test steps:** open the app → create a sale and complete it (اعتماد) → open "التقارير المحاسبية" for a period containing it: the "التكلفة تقديرية" warning must **not** appear and COGS must equal quantity × the product's cost at the time of sale. Then approve a purchase that changes the product's cost → recompute the same period: COGS and net profit must be unchanged. A period containing pre-upgrade invoices must still show the warning, now stating the number of estimated lines/invoices.

**Risks:** low. The DDL upgrade is additive and idempotent; legacy rows are correctly reported as estimated rather than silently re-costed. Residual: lines written by an older binary between upgrade and first posting are `NULL` (by design); product cost remains the *latest* purchase price (unchanged methodology).

## Latest: Sales invoices history showed no invoices — FIXED (branch `fix-sales-history`)
**Date:** 2026-10-24
**Symptom:** "سجل فواتير المبيعات" listed nothing (only the empty state) while the purchase history listed completed purchases, even though POS sales were completed.

**Root cause (confirmed, code-only — no DB touched):**
- [SalesInvoicesHistoryView.xaml](file:///e:/windows_app_csharp/CarPartManager/Views/SalesInvoicesHistoryView.xaml#L141) (old line 141) set a **local** `Visibility="{Binding IsLoading, Converter={StaticResource BoolToVisConverter}}"` on the DataGrid. `BooleanToVisibilityConverter` maps `false` → `Collapsed`, and a **local value outranks Style triggers** in WPF precedence, so as soon as loading finished (`IsLoading = false`) the grid was permanently `Collapsed` and the `ShowEmptyState` border was all the user ever saw. The purchase history view has no such local binding (only the loading `DataTrigger`), which is why it worked.
- Ruled out (verified by reading code, not the DB): the sales VM is structurally identical to the purchase VM; `SqliteSalesInvoiceService.GetAllInvoicesAsync` has **no** status/date/IsActive filter, **no** join to `Customers` (walk-in rows with `CustomerId = null` are returned), and orders `InvoiceDate DESC, Id DESC`; the POS save path `SalesInvoicesViewModel.ExecutePostInvoiceAsync` → `SqliteSalesInvoiceService.PostInvoiceAsync` writes to the `SalesInvoices` table with `Status = "معتمدة"`; navigation in `MainViewModel.NavigateTo` loads then sets `CurrentViewModel` symmetrically for both history pages; `DbContext` is created per call (`using`), no stale context; the VM already shows an Arabic `MessageBox` + status message on failure (no silent catch).

**Fix (smallest safe change, no schema/invoice/inventory/accounting change):**
- `Views/SalesInvoicesHistoryView.xaml` — removed the local `Visibility` binding from the DataGrid and kept a single `DataTrigger IsLoading=True → Collapsed` (mirrors the purchase history view).
- `ViewModels/SalesInvoicesHistoryViewModel.cs` — normalise a null/empty `CustomerName` to "عميل نقدي" when loading (in-memory projection only, no DB write), so legacy / walk-in invoices never render a blank customer cell. Search and details are unchanged.

**Build:** `dotnet build CarPartManager.csproj` → **Build succeeded, 0 Error(s)** (8 warnings = 4 pre-existing `CS8602` in `InventoryViewModel.cs` 217–220, duplicated for the `_wpftmp` project). Not runtime-tested (no `.db` touched, app not run).

**Manual test steps:** open the app → "سجل فواتير المبيعات" → the grid must list all completed (معتمدة) sales invoices newest first, including walk-in ones showing "عميل نقدي"; type in the search box (invoice no. / customer / phone / notes); click a row to select it; click "تحديث السجل".

**Risk:** low — a XAML visibility correction plus an in-memory display fallback. The empty-state border still shows when there genuinely are no rows.

## Latest (2nd pass): "STILL shows no invoices" — why, and how it is now proven
**Date:** 2026-10-24
**Finding:** the fix above is correct but **was never in the binary that was tested**. `git branch --contains 626d8f1` → **only `fix-sales-history`** (`main`, `feature-expenses`, `feature-reports-a`, `feature-reports-b` do **not** contain it). The newest executable before this pass (`bin/Debug/net10.0-windows/CarPartManager.exe`, 21:25:40) was produced **30 s after** the `feature-expenses` commit `7e2c1db` (21:25:10) — a branch without the fix — so the run still contained the local `Visibility="{Binding IsLoading, Converter=BoolToVisConverter}"` on the DataGrid that collapses it whenever `IsLoading == false` (i.e. always after load). The two fixed files carry the 21:29:10 checkout timestamp of the later switch back to this branch. **To verify: build/run from `fix-sales-history` (or merge it into `main`).**
**Re-verified code path (no edits, no `.db` touched):** the history query `SqliteSalesInvoiceService.GetAllInvoicesAsync` (`Services/SqliteSalesInvoiceService.cs:45-61`) applies **no** status/date/soft-delete/branch filter and **no** join to `Customers` — walk-in rows (`CustomerId = null`) are returned and shown as "عميل نقدي"; the POS save path `PostInvoiceAsync` (`:195-373`) writes `SalesInvoices` + `SalesInvoiceItems` with `Status = "معتمدة"` (Arabic, same vocabulary), `PostedDate = DateTime.Now`, `InvoiceDate = invoice.InvoiceDate`; the working purchase query is structurally identical; the load **is** triggered on page open (`ViewModels/MainViewModel.cs:163-168`) on the same VM instance bound to the view; the catch is not silent.
**Changes this pass:**
- `CarPartManager.Tests/SalesHistoryQueryTests.cs` (new, in-memory SQLite — not EF InMemory): (1) saves a walk-in sale and a customer sale through the **real** `PostInvoiceAsync` path and asserts both appear in `GetAllInvoicesAsync` with `Status = "معتمدة"`, newest first, walk-in `CustomerId == null` → "عميل نقدي"; (2) simulates an old DB missing `SalesInvoices.CustomerId/CustomerAddress/CancellationDate/CancellationReason`, runs the real startup `DbInitializer.InitializeAsync` and asserts the columns are restored and the old invoice still loads.
- `Services/SqliteSalesInvoiceService.cs:13` — `CreateContext()` is now `protected virtual` (same pattern as `SqliteStocktakeService`) so tests can point the service at a test connection.
- `ViewModels/SalesInvoicesHistoryViewModel.cs` — load failure now shows the real Arabic error including inner exceptions (`Describe(ex)` walks `InnerException`), and the grid is explicitly bound to the same list; footer counter added.
- `Views/SalesInvoicesHistoryView.xaml` — footer `عدد الفواتير: N` in the status bar (bound to `InvoicesHistory.Count`).

**Build/Test:** `dotnet test CarPartManager.Tests\CarPartManager.Tests.csproj` → **Passed! Failed: 0, Passed: 9** (7 stocktake + 2 new), EXIT=0. Main project builds with 0 errors (8 pre-existing `CS8602` warnings in `InventoryViewModel.cs` 217–220).

## Status
**Date:** 2026-10-23 (updated for Part 2)
**Branch:** `wip-inventory`
**Task:** "المخزون والجرد" (Inventory & Stocktake) Screen — Part 1 + Part 2
**Status:** ✅ Part 1 (Stock Overview + Movements) on `main`. ✅ Part 2 (Stocktake) implemented on `wip-inventory`; **build succeeds, 0 errors** (8 warnings = 4 pre-existing `CS8602` in `InventoryViewModel.cs` 217–220, deferred). Not yet runtime-tested (no `.db` touched).

## Overview
The Inventory screen has three tabs:
1. **Stock Overview** - Summary cards + DataGrid with real-time search and filters
2. **Stock Movements** - Append-only ledger with manual adjustment and consistency check
3. **Stocktake (الجرد)** - Session lifecycle + counting + approval (Part 2, see below)

## Part 2: Stocktake (الجرد) — branch `wip-inventory`
Implemented on `wip-inventory` on top of Part 1. Build: succeeded, 0 errors.

### What was implemented
- **Models:** `StocktakeSessionModel` (lifecycle مسودة→جاري العد→مراجعة→معتمد/ملغاة, scope, dates) + `StocktakeLineModel` (BookQuantity snapshot, CountedQuantity, CostPrice, computed Difference/DifferenceValue/DifferenceType). `Lines` navigation (`List<StocktakeLineModel>`) added on the session.
- **Service:** `IStocktakeService` + `SqliteStocktakeService` — CreateSession (full/category scope, snapshots book qty), StartCounting, SubmitForReview, ApproveSession, CancelSession, GetLines, GetLineByProduct/Barcode, UpdateLine, CountProduct, CountByBarcode, HasMovementsAfterSnapshot, GetSessionSummary.
- **Approval rules (verified by code review):** single DB transaction; status must be `مراجعة` (can't approve twice); per counted line with a difference → `product.Quantity += Difference` + an append-only `تعديل جرد` `InventoryMovementModel`; negative-stock guard throws + rolls back; `DataEvents.RaiseInventoryOrInvoicesChanged()` after commit.
- **UI:** Stocktake tab in `InventoryView.xaml` (session header + action buttons, barcode-count row, lines DataGrid, history DataGrid) + new `CreateStocktakeSessionWindow` dialog (XAML + code-behind) — moved out of the ViewModel (no WPF controls in the ViewModel).
- **DbContext:** `StocktakeSessions`/`StocktakeLines` DbSets + `OnModelCreating` config; `HasMany(s => s.Lines).WithOne().HasForeignKey(l => l.SessionId).OnDelete(Cascade)` added so `Include(s => s.Lines)` maps to the real `SessionId` column (was missing → would have used a shadow FK).
- **Schema:** tables via `DbInitializer.EnsureStocktakeTablesCreatedAsync` (idempotent `CREATE TABLE IF NOT EXISTS`); `StocktakeLines.SessionId` + FK `ON DELETE CASCADE` already present. No EF Core migration (project uses `EnsureCreated` + raw DDL, not EF migrations).

### Build-fix history (commits on wip-inventory)
- `01b3f1f` — WIP: agent stopped mid-task (quota), unverified (interrupted stocktake work; did NOT compile — 15 errors)
- `5d0bb56` — Step 1: moved session-creation dialog out of ViewModel into `CreateStocktakeSessionWindow` XAML (fixed 14 errors)
- `df4022e` — Step 2: added `Lines` navigation to `StocktakeSessionModel` (fixed the 15th build error)
- `ec393ac` — Step 2 (complete): configured `StocktakeSession.Lines` EF relationship in `CarPartsDbContext` (fixes runtime `Include`; schema-neutral)

### SQLite database path (for testing)
`CarPartsDbContext.GetDatabaseFilePath()` → `%LOCALAPPDATA%\CarPartManager\carparts.db` (`C:\Users\samys\AppData\Local\CarPartManager\carparts.db`), opened in `OnConfiguring` via `UseSqlite("Data Source={path}")`. To test against a copy: copy the `.db` elsewhere and temporarily repoint the path. Do not run against the live DB.

### Test project
None exists (single `CarPartManager.csproj`, WinExe). The requested EF tests (in-memory SQLite) could not be added without creating a test project + new packages (test SDK/framework) — forbidden. Not added.

---

**Date:** 2026-10-24
**Task:** "التقارير المحاسبية" (Accounting Reports) screen — **Part B**: 4 additional read-only tabs (Purchases, Inventory valuation, Customer debts, Item profitability)
**Branch:** `feature-reports-b` (from `feature-reports-a`); Part A committed on `feature-reports-a`
**Status:** ✅ Implementation COMPLETE — build verified, manual UI testing pending
**Latest fix:** ✅ runtime SQLite errors on the Expenses screen fixed (see "0. Bug fix" below)

---

## 0. Bug fix (2026-10-24): SQLite errors on the Expenses screen

**Symptoms:** opening the screen → "تعذر تحميل تصنيفات المصروفات" (+ SQLite error); adding a category →
"تعذر إضافة التصنيف" (+ SQLite error).

**Confirmed root cause:** `ExpenseCategoryModel.ExpensesCount` (a display-only counter) was mapped by
convention because it was **missing from the `Ignore(...)` list** in `CarPartsDbContext.OnModelCreating`
(`ExpenseCategory` block). EF therefore expected a real column `ExpensesCount` in the `ExpenseCategories`
table, but the raw DDL in `DbInitializer.EnsureExpenseTablesCreatedAsync()` only creates `Id, Name, IsActive,
CreatedDate`. `EnsureCreatedAsync()` does nothing on an existing database (the tables come from the raw DDL),
so the column was missing → `SQLite Error 1: no such column: c.ExpensesCount` on `SELECT`, and
`no such column named ExpensesCount` on `INSERT`. A fresh database worked because `EnsureCreatedAsync()`
builds the schema from the model (including the bogus column) — which is why the bug only appeared on an
existing database. `ExpensesCount` is never used in SQL; it is assigned in memory in
`SqliteExpenseService.GetCategoriesAsync()`.

**Fix (2 commits, no schema change, no DDL change needed):**
1. `4347d05` — `Data/CarPartsDbContext.cs`: added `entity.Ignore(c => c.ExpensesCount);` so the EF model
   matches the DDL (the property stays an in-memory, computed value as documented on the model). Works for
   both existing and fresh databases; existing rows/tables are untouched.
2. `6f1fe84` — `Services/SqliteExpenseService.cs`: added a private `Describe(Exception)` helper that walks to
   the innermost exception and shows that message in the Arabic error text (EF's `DbUpdateException` message
   — "An error occurred while saving the entity changes" — hid the real SQLite cause). Applied to all 12
   catch wrappers; validation errors still surface as-is.

**Verified:** `dotnet build CarPartManager.csproj` → Build succeeded, 0 Warning(s), 0 Error(s) after each commit
(static verification of the model↔DDL mapping for all three expense entities; `ExpensesCount` was the only
property missing from the DDL). No `.db` file was read/written, the app was not run, no DDL was changed.
No test project exists on this branch, so no automated test was added (no packages added).
**Note:** READ-ONLY screen — it never modifies data. Part A's filter, service, styles and rules are reused; no schema change, no `.db` touched, app not run.

---

## Reports — Part A (2026-10-24)

### Prerequisites verified
- Expenses module present (committed on `feature-expenses`, documented in the appendix below).
- Sales-history bug fix present in [SalesInvoicesHistoryViewModel.cs](file:///e:/windows_app_csharp/CarPartManager/ViewModels/SalesInvoicesHistoryViewModel.cs) (try/catch + Arabic error message).

### Inspection findings that drove the design
- Invoice lifecycle: statuses are Arabic strings — `مسودة` (draft), `معتمدة` (completed/posted), `ملغاة` (cancelled). Only `معتمدة` is counted anywhere.
- Sales lines: `SalesInvoiceItemModel` stores ProductId / PartNumber / Barcode / ProductName / Quantity / UnitPrice and **no unit cost at sale time** → COGS is **ESTIMATED** from the product's current `Products.PurchasePrice`. A visible Arabic warning ("التكلفة تقديرية") is shown. Schema and invoice logic were **not** changed.
- Returns: `SalesReturnModel.Status` (`مسودة`/`معتمدة`) with `TotalAmount`; per-line `ItemCondition` — "صالح للبيع" (restocked) vs "تالف" (damaged → `DamagedQuantity`, not restocked) vs "استبعاد نهائي من المخزون". Only sellable returns reduce COGS.
- Expenses: `ExpenseModel.IsCancelled` + `CancellationReason`/`CancellationDate`; cancelled rows are excluded from every total.
- No chart library and no export/print library exist → **tables only, no chart** (a note is displayed in the UI). No test project exists on `main` → no tests added, no packages added.

### Formulas implemented (money = decimal, rounded only at presentation)
- Net sales = Σ(`SubTotal − Discount`) of `معتمدة` invoices − Σ(`TotalAmount`) of `معتمدة` returns, in the period.
- COGS = Σ(qty × current `PurchasePrice`) over posted invoice lines − Σ(qty × same price) over **sellable** returned lines → **estimated**, not exact.
- Gross profit = Net sales − COGS. Net profit = Gross profit − Σ(active, non-cancelled expenses in the period).
- Previous period = identical-length window immediately before the selected period; amount and % change shown.
- Date boundaries are **inclusive start / exclusive next-day end** (a full day is never cut off).

### Files
- New: `Services/IReportsService.cs`, `Services/SqliteReportsService.cs`, `Models/ProfitSummaryModel.cs`, `Models/SalesDailyReportRow.cs`, `Models/TopProductReportRow.cs`, `Models/CustomerSalesReportRow.cs`, `Models/ExpenseReportModel.cs`, `ViewModels/ReportsViewModel.cs`, `Views/ReportsView.xaml` (+ `.xaml.cs`).
- Modified: `App.xaml` (DataTemplate), `ViewModels/MainViewModel.cs` (navigation branch + field).
- No `.db` touched, no schema change, no new packages, calculations live in the reports service (MVVM).

### Build
`dotnet build CarPartManager.csproj` → **Build succeeded, 0 Warning(s), 0 Error(s)**.

### TODO — Part B
- ✅ Done — see "Reports — Part B" below.

---

## Reports — Part B (2026-10-24)

### Prerequisites verified
- Part A present (tabs structure, global date filter, `IReportsService`/`SqliteReportsService`) — committed on `feature-reports-a`.
- Inventory screen merged on `main` (`StockMovement` ledger, `MinimumStock`/`ProductModel.IsLowStock`, `IInventoryService`).

### Tabs added (all READ-ONLY, reuse Part A filter/service/style)
1. **المشتريات** — posted (`معتمدة`) purchase invoices only; total + count cards, comparison to the equal-length previous window, daily table (cross-checked against its own total), by-supplier table (shown only when supplier data exists, otherwise a visible Arabic note), top purchased products.
2. **تقييم المخزون** — current state (NOT date-filtered, labeled in the tab note with a generation timestamp). Value = Σ(available qty × current cost). Cards: total value, product count, below min level, out of stock, stagnant. Tables: top by value, below min level, out of stock, stagnant. `StagnantDays` defaults to 60 and is adjustable from a ComboBox (30/60/90/180); changing it reloads only this tab.
3. **مديونيات العملاء** — customers with a positive balance, balance, last payment date, oldest unpaid invoice date, total debt. Balance **reuses** `SqliteCustomerService.GetAllCustomersAsync()` (the Customers screen formula: opening + Σ(GrandTotal − PaidAmount of `معتمدة`) − Σ(non-cancelled payments), floored at 0) — not reimplemented. Shows the formula note + "not affected by the date filter".
4. **ربحية الأصناف** — per product in the period: quantity sold, sales value, cost, profit, margin %; plus best/worst tables. Item sales value distributes the invoice discount proportionally (`(SubTotal − Discount)/SubTotal × qty×price`) and subtracts returns; cost uses the current `Products.PurchasePrice` → **التكلفة تقديرية** warning shown.

### Rules respected
- No schema changes, no `.db` touched, app not run, no new packages. Aggregation is DB-side with projections + `AsNoTracking`; decimal Sum/multiply/compare done in memory after projection (SQLite cannot aggregate `decimal`). Draft/cancelled excluded everywhere. Async + loading overlay. Arabic RTL, empty states, no overlapping, resizes cleanly (Style triggers / overlay pattern — no local `Visibility` bindings).

### Cross-checks (visible warnings, not silent)
- Purchases: daily-table total is re-summed from `PurchasesDaily` and shown next to the card total.
- Inventory: `Σ(rows.StockValue)` is recomputed and compared to the report total; mismatch → visible data warning.
- Item profitability: total profit is compared to Part A's gross profit; mismatch → visible warning naming probable causes (discount on a zero-total invoice, return value ≠ its lines, inconsistent invoice data).

### Files
- New: `Models/PurchaseReportModel.cs`, `Models/InventoryValuationModel.cs`, `Models/CustomerDebtReportModel.cs`, `Models/ItemProfitabilityModel.cs`.
- Modified: `Services/IReportsService.cs` (+4 methods), `Services/SqliteReportsService.cs` (4 implementations + helpers), `ViewModels/ReportsViewModel.cs` (per-tab state/loaders/apply + cross-checks), `Views/ReportsView.xaml` (+4 `TabItem`s).

### Build
`dotnet build CarPartManager.csproj` → **Build succeeded, 0 Warning(s), 0 Error(s)** (after the service/models step, after the ViewModel step, and after the XAML step).

### TODO
- Storing a real unit cost on each sales line in a future task would turn COGS/item cost from estimated into exact.

---

## Appendix — Previous task (2026-10-24): Expenses module — COMPLETED

### 1. Inspect-first findings

- **The Expenses screen was an empty placeholder**: `MainViewModel.NavigateTo` fell through to
  `PlaceholderViewModel` for tag `"Expenses"`; no `ExpensesViewModel`/`ExpensesView` existed.
- **No DI container**: services are constructed directly (`new SqliteXxxService()`) inside ViewModels.
  `CarPartsDbContext` is instantiated per call (`CreateContext() => new CarPartsDbContext()`), context
  lifetime is per-operation.
- **Schema mechanism**: the project does **not** use EF Core migrations. `DbInitializer.InitializeAsync()`
  calls `EnsureCreatedAsync()` then runs **idempotent raw DDL** (`CREATE TABLE IF NOT EXISTS ...`) plus
  idempotent seed helpers. The new Expenses tables follow exactly this mechanism (no migration files added).
- **Payment methods already exist as constant lists** in other ViewModels (e.g. `CustomersViewModel`):
  `نقدي` (default), `شبكة / فيزا`, `تحويل بنكي`, `شيك`. Reused as-is (no new table).
- **No real user/auth system** exists — only a display string (`MainViewModel.CurrentUser`). Expenses
  therefore store user as a nullable text field (`CreatedBy` / `ModifiedBy`), and the UI shows it when present.
- **SQLite cannot aggregate `decimal` in SQL** — the established project pattern (see `SqliteDashboardService`)
  is `AsNoTracking().ToListAsync()` then `Sum(...)` **in memory**. `SqliteExpenseService` follows this.
- **No test project exists on `main`** (`CarPartManager.Tests` on disk contains only stale `bin/obj`
  artifacts, zero tracked files). Per the rules, no test project and no packages were added to the main project.

## 2. Implementation

### Data layer
- Entities: `ExpenseModel`, `ExpenseCategoryModel`, `ExpenseHistoryModel` (+ POCO `ExpenseSummaryModel`).
- `CarPartsDbContext`: 3 DbSets and model config — money `decimal(18,2)`, index on `ExpenseDate`,
  composite index on `(CategoryId, ExpenseDate)`, index on category `Name`; FK `Expense.CategoryId →
  ExpenseCategory.Id` with `DeleteBehavior.Restrict` (a category with expenses can never be hard-deleted);
  FK `ExpenseHistory.ExpenseId → Expense.Id` with `Cascade`. Computed/display-only properties are `Ignore`d.
- `DbInitializer`: `EnsureExpenseTablesCreatedAsync()` (idempotent `CREATE TABLE IF NOT EXISTS` for the three
  tables + indexes) and `SeedDefaultExpenseCategoriesAsync()` (idempotent seed using `NameNormalizer`):
  إيجار، كهرباء ومياه، مرتبات، نقل وشحن، صيانة، تسويق، مستلزمات، أخرى. Wired into `EnsureTablesCreatedAsync()`.
- `Services/NameNormalizer.cs`: `Normalize` (trim + collapse inner whitespace) and `AreEquivalent`
  (OrdinalIgnoreCase) — used for duplicate-category detection.
- `Services/IExpenseService.cs` + `Services/SqliteExpenseService.cs`: all data logic (DB-side filtering,
  `AsNoTracking` for read lists, async everywhere). Exceptions are wrapped in `InvalidOperationException`
  with clear Arabic messages (never swallowed).

### Business rules implemented
- **Cancel, never delete**: `CancelExpenseAsync(id, reason, user)` sets `IsCancelled`, `CancellationDate`,
  `CancellationReason`; the row is kept. Cancelled expenses are excluded from *every* total (summary cards,
  filtered grid total, top category). No hard delete API exists.
- **Categories**: add + deactivate/reactivate; never hard-deleted when used (`Restrict` FK + UI refuses);
  duplicate names prevented via normalized comparison.
- **Edit audit (chosen minimal mechanism)**: alongside `CreatedAt/ModifiedAt` (+ `CreatedBy/ModifiedBy`
  when a user is available), every edit and every cancellation appends a row to `ExpenseHistories`
  recording change type, previous/new amount, previous/new category name, previous/new date and a reason.
  This is the "small history table" option — the simplest mechanism that guarantees the original data is
  never silently lost, with no impact on the Expense table shape. History is viewable in the UI
  (per-row "السجل" action → collapsible audit panel).
- **Isolation**: expenses touch only their own tables — no inventory, customer, or sales logic is referenced.

### UI/UX
- `Views/ExpensesView.xaml`: Arabic RTL, 4 metric cards (اليوم / الشهر الحالي / الشهر السابق / أعلى تصنيف
  هذا الشهر), filter bar (search, category, payment method, date range, include-cancelled, clear),
  collapsible category-management panel, collapsible audit panel, grid with loading/empty/error/success
  Arabic states and a status bar showing the filtered **active** total and count.
- `Views/ExpenseDialogWindow.xaml` (add/edit) and `Views/CancelExpenseDialogWindow.xaml` (cancel with
  mandatory reason) follow the existing dialog pattern (`RequestClose` + `ShowDialog() == true`).
- Dialog: date defaults to today; **Save enables the moment category + positive amount are valid**
  (`UpdateSourceTrigger=PropertyChanged` on the amount box; `CanSave` re-evaluated and
  `SaveCommand.RaiseCanExecuteChanged()` called from the setters). Optional fields (description, reference,
  notes) never block saving. Invalid input shows a clear Arabic message. Arabic-Indic digits are accepted in
  the amount field via `PhoneHelper.NormalizeDigits` + culture-invariant parsing (`٫`/`,` → `.`).
- The grid uses a `DataGrid.Style` with `DataTrigger`s for visibility — **not** a local `Visibility` value —
  to avoid the dependency-property-precedence bug previously found in `SalesInvoicesHistoryView.xaml`.

## 3. How the schema is created

No EF migrations. On startup `DbInitializer.InitializeAsync()` runs `EnsureCreatedAsync()` (which, for a
pre-existing database, is a no-op) and then `EnsureExpenseTablesCreatedAsync()`, which executes raw
`CREATE TABLE IF NOT EXISTS` / `CREATE INDEX IF NOT EXISTS` statements, followed by
`SeedDefaultExpenseCategoriesAsync()`. All statements are additive and idempotent: existing data is
untouched, no `.db` file was deleted or recreated, and repeated runs are safe.

## 4. Changed / new files

```
New:
  Models/ExpenseModel.cs
  Models/ExpenseCategoryModel.cs
  Models/ExpenseHistoryModel.cs
  Models/ExpenseSummaryModel.cs
  Services/NameNormalizer.cs
  Services/IExpenseService.cs
  Services/SqliteExpenseService.cs
  ViewModels/ExpensesViewModel.cs
  ViewModels/ExpenseDialogViewModel.cs
  ViewModels/CancelExpenseDialogViewModel.cs
  Views/ExpensesView.xaml (+ .xaml.cs)
  Views/ExpenseDialogWindow.xaml (+ .xaml.cs)
  Views/CancelExpenseDialogWindow.xaml (+ .xaml.cs)

Modified:
  Data/CarPartsDbContext.cs     (3 DbSets + model configuration)
  Data/DbInitializer.cs         (expense tables + default category seed)
  ViewModels/MainViewModel.cs   (cached _expensesViewModel + navigation branch)
  App.xaml                      (ExpensesViewModel -> ExpensesView DataTemplate)
  CarPartManager.csproj         (excludes the stale CarPartManager.Tests bin/obj artifacts from the glob,
                                 which otherwise caused CS0579 duplicate assembly attributes)
```

## 5. Build result

`dotnet build CarPartManager.csproj` → **Build succeeded, 0 Warning(s), 0 Error(s)** (exit code 0).
No test project exists on `main`, so **no automated tests were added** (adding them would require adding
packages, which the task forbids).

## 6. Verification

### Verified (by reading code + build)
- Project compiles cleanly with all new files.
- Navigation: `Expenses` nav item now resolves to the real `ExpensesViewModel`; the placeholder branch and
  the placeholder description entry for `Expenses` were removed.
- `App.xaml` maps `ExpensesViewModel` → `ExpensesView`.
- All read queries filter in the database and use `AsNoTracking`; cancelled rows are excluded from every
  total (summary queries filter `IsCancelled == false` in SQL; the grid total additionally sums only
  non-cancelled rows in memory).
- Decimal parsing/aggregation follows the project conventions; money values are `decimal`.

### Manual tests required (not executed — the app must not be run)
1. Open المصروفات → summary cards and grid load without error; empty state appears when there is no data.
2. مصروف جديد → Save stays disabled until a category is chosen and the amount is a positive number; enter
   an Arabic-Indic amount (e.g. `١٢٥٫٥٠`) and confirm it saves as `125.50`.
3. Save a new expense → it appears in the grid; today's card and the month card increase.
4. Edit that expense → change the amount/category → the per-row "السجل" panel shows the old values.
5. Cancel an expense with a reason → status becomes "ملغاة", and **all** totals drop accordingly; the row
   is still present when "عرض الملغاة" is checked.
6. إدارة التصنيفات → add a category, try adding a near-duplicate (e.g. extra spaces / different case) and
   confirm it is rejected; deactivate a used category and confirm it cannot be deleted and still resolves
   for its existing expenses.
7. Filters: search by description, category, payment method, date range, include-cancelled — confirm the
   filtered total reflects active rows only and that an invalid range (from > to) shows an Arabic message.
8. Resize the window and confirm no overlapping columns and correct text trimming + tooltips.

## 7. Risks & notes

### Not Changed
- No EF Core migration file (table already exists)
- No existing quantity-changing logic altered (only added movement recording)
- No negative stock allowance (pre-existing validation kept)
- No stocktake functionality in Part 1 scope (now implemented in Part 2, see above)
- No location/warehouse columns (designed for future extensibility)

### Expenses — notes
- Additive schema only; existing tables/data untouched. No `.db` file was opened, modified, or deleted.
- `EnsureCreatedAsync()` will not alter an existing database, which is exactly why the new tables are
  created through the explicit idempotent DDL block.
- Expenses store the user as a text label because the app has no authentication; swapping in a real user
  table later only requires changing that string source.
- `CarPartManager.csproj` now excludes `CarPartManager.Tests\**` from the default globs. The stale folder
  holds only `bin/obj` artifacts (no source, no `.csproj`); it can be deleted by hand at any time, after
  which the exclusion becomes harmless.

## Remaining TODO
1. **Stocktake Tab** — ✅ DONE (Part 2, above). Open follow-ups:
   - **`ApproveSessionAsync` snapshot-vs-current bug:** approval does `product.Quantity += (CountedQuantity − BookQuantity)`, where `BookQuantity` is the frozen snapshot but `product.Quantity` is the CURRENT live quantity. If sales/purchases occurred after the snapshot, the net interim movement is effectively double-counted (final = current + counted − book = counted + net-interim), so the approved quantity drifts from the physical count. `HasMovementsAfterSnapshotAsync` only warns (no block/recompute). Likely fix: set `product.Quantity = CountedQuantity` (count authoritative) and log the discrepancy separately.
   - **Negative-stock guard can block a legitimate shortage:** because the snapshot-delta is applied to the already-reduced current quantity, a real shortage can push current below 0 → throw + rollback the whole approval, so the shortage is never recorded. Fixing the snapshot-vs-current bug above should resolve this.
   - **Runtime test coverage:** no automated tests for the stocktake flow (see "Test project" above). Future: add a test project (new packages) with in-memory SQLite covering `Include(Lines)` via SessionId, approval movements/quantities, double-approve blocked, uncounted lines untouched, consistency check after approval.
2. **CS8602 warnings** — 4 pre-existing null-deref warnings in `InventoryViewModel.cs` (217–220), explicitly deferred (out of scope).
3. **Stagnant Stock Filter** - Add filter for products with no sales in N days (requires last-sale-date tracking, skipped as optional)
4. **Paging/Virtualization** - Consider for movements grid if dataset grows large (not needed for current scale)

## 8. Remaining TODO

1. Attach receipts/attachments to an expense (not requested in this task).
2. Expense reporting/export (monthly breakdown by category, CSV/print) — belongs to the Reports screen.
3. Batch operations (bulk cancel/re-categorise) — deferred; not needed at the current scale.
4. Optional: add a dedicated test project for expense totals/uniqueness/cancel-flow (requires a new test
   project + packages, which was out of scope here).

---

## Appendix — Previous task (2026-10-23): "المخزون والجرد" (Inventory & Stocktake, Part 1) — COMPLETED

Implemented the Inventory screen with two tabs: **Stock Overview** (summary cards + DataGrid with real-time
search/filters) and **Stock Movements** (append-only ledger with manual adjustment + consistency check).
Stocktake itself was reserved for a future task.

Quantity-change paths already hooked: purchase approval/cancellation, sales approval/cancellation, sales
returns (sellable + damaged), purchase returns; plus newly hooked: manual product edit
(`SqliteProductService.UpdateProductAsync`, now transactional and movement-recording).
Business rules verified then: drafts do not affect inventory, negative stock is not allowed, movements are
append-only, and all quantity changes happen in atomic transactions.

New files (that task): `Services/IInventoryService.cs`, `Services/SqliteInventoryService.cs`,
`ViewModels/InventoryViewModel.cs`, `Views/InventoryView.xaml` (+ `.xaml.cs`).
Modified: `App.xaml`, `ViewModels/MainViewModel.cs`, `Services/SqliteProductService.cs`,
`Data/DbInitializer.cs` (`BackfillOpeningStockMovementsAsync`, idempotent "رصيد افتتاحي" backfill),
`Styles/Controls.xaml` (`CleanTabControlStyle`, `BooleanToVisibilityConverter`).
No EF Core migration file was needed then either — the table already existed.
