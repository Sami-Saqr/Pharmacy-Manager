# PharmacyManager — Handover Document

> **المرجع الكامل للخطة:** [PHARMACY_PLAN.md](PHARMACY_PLAN.md)
> **الأرشيف التفصيلي:** [HANDOVER_ARCHIVE.md](HANDOVER_ARCHIVE.md) — اقراه بس لو محتاج تفصيل قديم

---

## 1. وصف المشروع
تحويل تطبيق **CarPartManager** (نظام كاشير + محاسبة + مخزون لمحل قطع غيار) إلى **PharmacyManager** —
برنامج صيدلية متكامل بنفس المعمارية. الهدف: إضافة حقول الأدوية، نظام الباتشات، FEFO، صلاحيات مدير/كاشير،
ونسخ احتياطي، مع الحفاظ على بنية الكود والداتابيز الحالية ما أمكن.

---

## 2. التقنية والمعمارية

| العنصر | التفاصيل |
|---|---|
| الـ Stack | WPF (.NET 10) + MVVM + SQLite / EF Core 10 |
| الـ UI | عربي RTL، `FlowDirection="RightToLeft"` على كل النوافذ |
| الـ packages | `Microsoft.EntityFrameworkCore.Sqlite 10.0.13`، `ClosedXML 0.105.1` |
| الداتابيز | `%LOCALAPPDATA%\PharmacyManager\pharmacy.db` |
| الـ DbContext | `PharmacyDbContext` في `Data/PharmacyDbContext.cs` |

### هيكل الفولدرات
```
PharmacyApp/
├── Data/             (PharmacyDbContext.cs, DbInitializer.cs)
├── Models/           (كل الـ models)
├── Services/         (interfaces + SQLite implementations)
├── ViewModels/       (كل الـ ViewModels)
├── Views/            (XAML + code-behind)
├── MVVM/             (ViewModelBase, RelayCommand, IHasUnsavedChanges)
├── Converters/
├── Styles/
├── PharmacyManager.Tests/   (xUnit، in-memory SQLite)
└── App.xaml / MainWindow.xaml
```

---

## 3. القواعد الثابتة (conventions)

- **Schema:** مفيش EF migrations. كل تغيير schema بـ `ALTER TABLE / CREATE TABLE IF NOT EXISTS` idempotent في `DbInitializer`.
- **DbContext:** instance واحد per-operation (`using var ctx = new PharmacyDbContext()`). مفيش shared context.
- **Transactions:** كل عملية مالية/مخزنية في `BeginTransaction` واحدة.
- **Decimal:** لا يتعمل aggregate في SQL. يتجمع في memory بعد `AsNoTracking().ToListAsync()`.
- **RTL:** `FlowDirection="RightToLeft"` على كل Window/UserControl. النصوص عربية.
- **COGS:** يتحسب من `UnitCostAtSale` المحفوظ على بند البيع (أو fallback للـ `PurchasePrice` الحالي + تحذير "التكلفة تقديرية").
- **Visibility في XAML:** لا تستخدم local `Visibility` binding على DataGrid — يسبب bug WPF precedence. استخدم `DataTrigger` فقط.
- **أخطاء async:** لا تُبلع الـ exceptions. كل catch يعرض رسالة عربية للمستخدم.

---

## 4. حالة الـ Build والـ Tests

| الحالة | التفاصيل |
|---|---|
| آخر build ناجح | `dotnet build` — 0 errors، 8 warnings (CS8602 في `InventoryViewModel.cs` L280-283، مؤجلة) |
| الـ Tests | **23/23 ناجحة** (على `PharmacyManager.Tests`) |
| الـ Tests project | `PharmacyManager.Tests/` — xUnit، in-memory SQLite |
| التطبيق | لم يُشغَّل الـ GUI تفاعلياً (بيئة CLI headless لا تدعم واجهة مرئية، يلزم تشغيله يدوياً للتحقق). |

---

## 5. الـ Bugs والـ Open Issues المعروفة

### Bugs مؤكدة
- **`ApproveSessionAsync` snapshot bug:** الجرد يطبّق `product.Quantity += (CountedQuantity - BookQuantity)` لكن `product.Quantity` هو الكمية الحالية (مش وقت الـ snapshot). لو حصل بيع بعد الـ snapshot تنجرف الكمية النهائية. الحل المقترح: `product.Quantity = CountedQuantity` مباشرة. (مرحلة 1A)
- **Negative-stock guard يبلوك شورتج مشروع:** نتيجة bug الـ snapshot، الكمية الحالية ممكن تنزل لتحت الصفر فتـ rollback الاعتماد كله.

### Open Issues — خارج نطاق Phase 0 (Phase 1A)
- حقل/عمود `CompatibleVehicles` في `Models/ProductModel.cs` و `Views/ProductsView.xaml` L247
- حقل/عمود `PartNumber` في `Models/ProductModel.cs`
- حقل/عمود `VehicleModel` في `Models/ProductModel.cs` و `Views/DashboardView.xaml` L211
- نص "ماركة وموديل السيارات المتوافقة" في `Views/ProductDialogWindow.xaml` L108
- بيانات seed في `DbInitializer.cs` (موردين + أصناف خاصة بقطع غيار) — تُستبدل في 1A
- `Services/MockProductService.cs` — بيانات mock خاصة بالسيارات
- `ViewModels/MainViewModel.cs` L37: StoreName placeholder خاص بقطع الغيار (يُستبدل بـ Settings في مرحلة 2)
- CS8602 warnings في `InventoryViewModel.cs` L280-283 — مؤجلة
- Products Excel export — مؤجل

---

## 6. تتبع المراحل

| المرحلة | الحالة |
|---|---|
| 0 — rename | ✅ مكتملة |
| 1A — drug fields | ⬜ |
| 1B-1 — Batches | ⬜ |
| 1B-2 — FEFO POS | ⬜ |
| 1B-3 — Expiry | ⬜ |
| 2 — Auth | ⬜ |
| 3 — Backup | ⬜ |
| 4 — Finish | ⬜ |

---

## 7. سجل التحديثات

### 2026-11-02 Phase 0 — استكمال وإصلاح الـ rename وهيكل الاختبارات
- اللي اتعمل: توحيد فولدر التست لـ PharmacyManager.Tests بـ git mv، واستثناؤه من csproj الرئيسي، وتحديث DbContext ومؤلف الإكسيل
- ملفات/جداول اتغيرت: PharmacyManager.csproj, PharmacyManager.Tests, PharmacyDbContext.cs, ExcelExportService.cs
- Schema changes: لا شيء (ممنوع في المرحلة 0)
- Build/Tests: ناجح 0 errors (8 warnings سابقة CS8602)، واختبارات xUnit ناجحة 23/23
- Open issues: حقول الأدوية وبج الجرد مؤجلة لـ 1A؛ لم يُشغَّل GUI لبيئة headless
- الخطوة الجاية: Phase 1A — حقول الدواء والـ Settings وإصلاح الجرد
