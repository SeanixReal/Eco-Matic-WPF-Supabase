# AI Handoff Documentation

## State of Project
- The goal is to resolve missing product images on the initial slots inside the `CustomerWindow.xaml` UI.
- Recent changes attempted to fix XAML dimension limits (`MinHeight`, `MaxWidth`) because the spacing works but pixels remain missing (ghosted).
- Refactored `Utilities/ImageLoader.cs` to leverage `pack://application:,,,/` and fallback to `AppContext.BaseDirectory` instead of using the older `CsvStorage.GetImageFullPath()`.
- The database outputs relative paths like `/Assets/Images/MrChips.png`.
- A temporary debug tracker (`System.IO.File.AppendAllText("img_binding.txt", ...)`) was injected into `CustomerWindow.xaml.cs` just prior to handoff but encountered file-lock/directory creation issues while testing.
- Dispensed items fade-out and message popups (z-index) have been successfully handled in previous changes.

## Next Steps for the Next AI
1. **Remove Tracers (If still present):** Evaluate `CustomerWindow.xaml.cs` `RefreshProducts()` implementation for any remaining `AppendAllText` injection logic and restore it to `slot.VendingItemImage.Source = ImageLoader.LoadProductImage(product.ImagePath);`.
2. **Determine Null vs Render:** Validate if `ImageLoader` is still returning `null` object instances due to URI misconfigurations or if WPF is rendering a `0x0` dimension image due to Grid formatting loops. Running and debugging `RefreshProducts()` is key.
3. **Verify Git History/File Moves:** Note that image files were historically mapped using `data/images` via legacy CSV code, but the codebase has drifted towards leveraging SQL Server with absolute paths like `/Assets/Images/...` pointing to locally tracked files.

## Environment Details
- WPF Application run via .NET 10.0 Windows.
- Targetting `net10.0-windows` architecture.