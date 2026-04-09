# PlateGuard UI/UX Action Plan: The Final Polish

The dark sidebar and typographic hierarchy updates have dramatically improved the "premium" feel of the application. However, based on the latest screenshots, there are a few critical alignment bugs and minor layout quirks holding it back from feeling completely native. 

Here is the exact technical roadmap to fix these final UI issues.

---

## 1. Global Vertical Alignment Bug (CRITICAL)
**Observation**: Across all screens, the text inside almost every interactive element is floating to the very top edge. This affects:
- The "Clear", "Refresh", and "Add Usage" buttons.
- The Date inputs and Search input in the History toolbar.
- The Settings text inputs ("Optional shop name").
- The Cancel and Delete buttons in the dialog.

**Technical Fix (`App.axaml`)**:
When we increased the `MinHeight` of buttons and textboxes globally to give them a modern desktop size, the internal text content didn't naturally center. You must explicitly apply `VerticalContentAlignment` to the global styles.
```xml
<!-- In App.axaml -->
<Style Selector="Button">
    <Setter Property="VerticalContentAlignment" Value="Center"/>
    <Setter Property="HorizontalContentAlignment" Value="Center"/>
</Style>

<Style Selector="TextBox">
    <Setter Property="VerticalContentAlignment" Value="Center"/>
</Style>

<Style Selector="ComboBox"> <!-- Fix the "All Promotions" dropdown text too -->
    <Setter Property="VerticalContentAlignment" Value="Center"/>
</Style>
```

---

## 2. Redundant Search Label on Home Screen
**Observation**: On the Search/Home screen, specifically right above the large `searchHeroBox`, there is a small blue text line that perfectly matches the placeholder text (`"Search by vehicle number, phone number..."`). This dual-labeling is redundant visual noise.
**Technical Fix (`Views/MainWindow.axaml`)**:
- Locate the `<TextBlock>` directly above the `searchHeroBox` (around line 176) and delete it entirely. The placeholder text inside the sleek text box is all the user needs.

---

## 3. History Date Filter Proportions
**Observation**: The "From" and "To" date text boxes in the History tab look squished compared to the large "Search by..." box. Once the vertical alignment bug (Issue #1) is fixed, they will still feel a bit tight.
**Technical Fix (`Views/MainWindow.axaml`)**:
- Add explicit horizontal padding so the typed dates don't touch the borders. 
- Example: For the `TextBox` elements handling the dates (around line 640), ensure they share the same internal padding as the main search box: `<TextBox Padding="12,0" .../>`.
- If Avalonia's `<DatePicker>` control is available instead of a standard `TextBox`, consider switching to it for native calendar dropdowns.

---

## 4. Delete Record Dialog Styling
**Observation**: In the Delete Record popup, the layout feels slightly disjointed. The buttons at the bottom ("Cancel" and "Delete") will benefit from the global centering fix (Issue #1), but the "Delete" button specifically looks like it has a washed-out pink background, which lacks contrast.
**Technical Fix (`App.axaml` or Dialog XAML)**:
- Instead of a pale pink background with light red text, a destructive action should either be a bold red button with white text, or an outlined red button. 
- Ensure the `<Button Classes="danger">` (or whichever style is applied to the Delete button) sets:
  ```xml
  <Setter Property="Background" Value="#DC2626"/> <!-- Strong Red -->
  <Setter Property="Foreground" Value="#FFFFFF"/> <!-- White Text -->
  ```

---

## 5. Better Disabled States
**Observation**: On the Home screen, the deactivated "Add Usage" button uses a gray background with gray text. It is very hard to read (poor contrast compliance).
**Technical Fix (`App.axaml`)**:
- Update the `:disabled` pseudo-class for Buttons so that the text remains readable.
  ```xml
  <Style Selector="Button:disabled">
      <Setter Property="Background" Value="#E2E8F0"/>
      <Setter Property="Foreground" Value="#94A3B8"/> <!-- Darker slate grey for contrast -->
  </Style>
  ```
