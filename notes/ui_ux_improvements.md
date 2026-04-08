# PlateGuard UI/UX Improvement Actions (Final Polish)

The overarching layout and structural design of the PlateGuard application are now in an excellent, professional state. However, the most recent visual review highlighted a persistent alignment issue specifically inside the application's text input fields. 

Below are the exact technical instructions to fix the floating text within the search bars and date inputs.

---

## 1. Fix Vertical Alignment in the Home Search Bar
**Issue**: Inside the main "Search / Home" view, the placeholder text (`"Find vehicle, phone, or owner..."`) and the user's typed text sit awkwardly near the top of the search box rather than being perfectly centered vertically. 
**Technical Cause**: The custom `searchHeroBox` style was given a large minimum height (`MinHeight="56"`), but Avalonia's default behavior for `TextBox` often aligns content to the top unless explicitly instructed otherwise through the `VerticalContentAlignment` property.
**Action**: Force vertical centering on the specific hero search box style.

**Location**: `src/PlateGuard.App/App.axaml` (Inside the Style map for `searchHeroBox`)
```xml
<!-- Before -->
<Style Selector="TextBox.searchHeroBox">
    <Setter Property="MinHeight" Value="56"/>
    <Setter Property="FontSize" Value="18"/>
    <!-- ... other setters ... -->
</Style>

<!-- After: Add VerticalContentAlignment -->
<Style Selector="TextBox.searchHeroBox">
    <Setter Property="MinHeight" Value="56"/>
    <Setter Property="FontSize" Value="18"/>
    <Setter Property="VerticalContentAlignment" Value="Center"/> <!-- FIX -->
    <!-- ... other setters ... -->
</Style>
```

---

## 2. Fix Vertical Alignment in History & Date Inputs
**Issue**: On the "History / Records" page, all the text inputs in the filter toolbar—the main search input, the "From yyyy-MM-dd" input, and the "To yyyy-MM-dd" input—are also suffering from the same vertical off-centering issue, making them look misaligned compared to the adjacent `ComboBox` and buttons.
**Technical Cause**: Just like the Hero Search box, the global `TextBox` style has had its `MinHeight` increased to `40` to match the buttons, but lacks the global vertical content centering command.
**Action**: Apply a global vertical centering fix to the base `TextBox` style so every input in the entire application (including Settings inputs and the Add Usage form) perfectly centers its text.

**Location**: `src/PlateGuard.App/App.axaml` (Inside the global `TextBox` Style)
```xml
<!-- Before -->
<Style Selector="TextBox">
    <Setter Property="MinHeight" Value="40"/>
</Style>

<!-- After: Add global vertical centering -->
<Style Selector="TextBox">
    <Setter Property="MinHeight" Value="40"/>
    <Setter Property="VerticalContentAlignment" Value="Center"/> <!-- FIX -->
</Style>
```

---

## Summary
By injecting `<Setter Property="VerticalContentAlignment" Value="Center"/>` into both the global `TextBox` style and the specific `TextBox.searchHeroBox` style in `App.axaml`, all textual content and placeholders inside inputs—including the main search, history search, and date pickers—will correctly snap to the vertical center. This will deliver the final, pixel-perfect finish the rest of the application now possesses.
