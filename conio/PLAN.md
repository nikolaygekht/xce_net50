# Modernization and Improvement Plan
## Gehtsoft.Xce.Conio Library

**Plan Created:** 2025-11-10
**Status:** Proposed
**Estimated Total Effort:** 3-4 weeks (single developer)

---

## Strategic Decision Point

**⚠️ CRITICAL FIRST STEP: Evaluate Terminal.Gui**

Before proceeding with modernization, we must evaluate whether Terminal.Gui (or other modern alternatives like Spectre.Console) can replace this custom library. This decision will determine whether to:
- **Option A:** Migrate to Terminal.Gui (if it meets all requirements)
- **Option B:** Modernize existing library (if unique capabilities are needed)

**See detailed feature comparison below**

---

## Phase 0: Strategic Evaluation (PRIORITY 1)

### Task: Comprehensive Terminal.Gui Feature Assessment
**Effort:** 2-3 days | **Impact:** Strategic direction

#### Evaluation Criteria

1. **Core Console I/O Capabilities**
   - ✓ Multi-platform support (Windows, Linux, macOS)
   - ✓ Direct Win32 Console API access (when needed)
   - ✓ VT/ANSI escape sequence support
   - ✓ True color (24-bit RGB) support
   - ✓ Canvas-based rendering with double buffering
   - ✓ Screen buffer manipulation

2. **Input Handling**
   - ✓ Full keyboard support with scan codes
   - ✓ Mouse support (buttons, wheel, movement)
   - ✓ Keyboard layout detection
   - ✓ Modifier key tracking (Shift, Ctrl, Alt)
   - ✓ Mouse capture mechanism
   - ✓ Input event listener pattern

3. **Windowing System**
   - ✓ CUA-style windowed interface
   - ✓ Hierarchical window model (parent-child)
   - ✓ Z-ordering (bring to front, send to back)
   - ✓ Modal dialog support
   - ✓ Focus management
   - ✓ Window invalidation/validation
   - ✓ Coordinate transformation (screen ↔ window ↔ client)
   - ✓ Moveable/resizable windows
   - ✓ Keyboard-based window management (Ctrl-Space shortcuts)

4. **Dialog System**
   - ✓ Modal dialogs
   - ✓ Tab navigation between controls
   - ✓ Enter/Escape handling
   - ✓ Alt+hotkey support
   - Required controls:
     - ✓ Label
     - ✓ Button
     - ✓ CheckBox
     - ✓ RadioBox
     - ✓ Single-line text input
     - ✓ ListBox
     - ✓ ComboBox
     - ✓ File dialog

5. **Menu System**
   - ✓ Popup menus
   - ✓ Menu bars
   - ✓ Keyboard navigation
   - ✓ Hot key support
   - ✓ Submenus

6. **Additional Features**
   - ✓ Clipboard integration (text copy/paste)
   - ✓ Color scheme system
   - ✓ Unicode support
   - ✓ Custom rendering/painting

7. **Quality & Ecosystem**
   - ✓ Active maintenance and development
   - ✓ Good documentation
   - ✓ Large user base / community
   - ✓ NuGet package availability
   - ✓ Test coverage
   - ✓ Performance characteristics
   - ✓ Extensibility (can we customize/extend?)

#### Deliverables

1. **Feature Comparison Matrix** (Terminal.Gui vs. Current Library)
2. **Migration Complexity Assessment** (if adopting Terminal.Gui)
3. **Gap Analysis** (features Terminal.Gui lacks)
4. **Performance Comparison** (if possible)
5. **License Compatibility Check** (MIT vs. current license)
6. **Recommendation Document** (build vs. buy decision)

#### Decision Criteria

**Migrate to Terminal.Gui IF:**
- ✅ Provides 90%+ of current functionality
- ✅ Missing features can be contributed upstream or worked around
- ✅ Migration effort < modernization effort
- ✅ Active maintenance with good community
- ✅ License compatible

**Modernize Current Library IF:**
- ❌ Terminal.Gui lacks critical features
- ❌ Architecture/design doesn't fit requirements
- ❌ Performance issues
- ❌ Customization needs can't be met
- ❌ Migration would break too much existing code

---

## Phase 1: Remove Legacy ConEmu Support & Modernize Platform Detection

**Priority:** HIGH | **Effort:** 2-3 days | **Impact:** Reduces complexity, improves maintainability

**Note:** Only proceed if strategic evaluation concludes we should modernize existing library.

### Tasks

#### 1.1 Remove ConEmu Integration
- [ ] Delete `Gehtsoft.Xce.Conio/Output/ConEmuConsoleOutput.cs` (347 lines)
- [ ] Remove `MarshalEx.cs` bit field system (only used by ConEmu)
- [ ] Remove `BitStructAttribute.cs` and `BitFieldAttribute.cs`
- [ ] Update `ConioFactory.cs` to remove ConEmu detection/creation
- [ ] Update test applications to remove ConEmu mode selection
- [ ] Remove ConEmu-related unit tests (if any)

**Files to Modify:**
- `Gehtsoft.Xce.Conio/ConioFactory.cs`
- `Gehtsoft.Xce.Conio.Test/Program.cs`
- `Gehtsoft.Xce.Conio.Win.Test/Program.cs`

**Files to Delete:**
- `Gehtsoft.Xce.Conio/Output/ConEmuConsoleOutput.cs`
- `Gehtsoft.Xce.Conio/Utils/MarshalEx.cs`
- `Gehtsoft.Xce.Conio/Utils/BitStructAttribute.cs`
- `Gehtsoft.Xce.Conio/Utils/BitFieldAttribute.cs`

#### 1.2 Consolidate Output Strategy
- [ ] Update `ConioFactory` to implement smart output selection:
  - **Windows 10+:** Prefer VT true color mode when available
  - **Windows Legacy:** Fall back to Win32ConsoleOutput (16-color)
  - **Linux/macOS:** Use VtTrueColorConsoleOutput
- [ ] Add Windows version detection for VT capability (Windows 10 1511+)
- [ ] Implement VT mode enable API call (kernel32.dll SetConsoleMode)
- [ ] Add graceful feature detection with fallbacks
- [ ] Remove manual mode selection where possible (auto-detect)

**Implementation Notes:**
```csharp
// Detect VT support on Windows 10+
private static bool SupportsVirtualTerminal()
{
    if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        return true; // Unix-like systems support VT

    // Check Windows version >= 10.0.10586 (Windows 10 1511)
    var version = Environment.OSVersion.Version;
    if (version.Major < 10) return false;
    if (version.Major == 10 && version.Build < 10586) return false;

    // Try to enable VT mode
    return TryEnableVirtualTerminalProcessing();
}
```

#### 1.3 Modernize Platform Detection
- [ ] Replace all `Environment.OSVersion.Platform` usage with `RuntimeInformation.IsOSPlatform()`
- [ ] Add `using System.Runtime.InteropServices;`
- [ ] Update platform-specific code paths

**Before:**
```csharp
if (Environment.OSVersion.Platform == PlatformID.Win32NT)
```

**After:**
```csharp
if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
```

### Acceptance Criteria
- [ ] ConEmu code completely removed
- [ ] All projects compile without errors
- [ ] Existing tests pass
- [ ] Factory auto-detects best output mode
- [ ] VT true color works on Windows 10+ without manual configuration

### Benefits
- Eliminates 600+ lines of legacy code
- Aligns with modern Windows Terminal standard
- Simplifies maintenance
- Reduces dependencies
- Improves cross-platform consistency

---

## Phase 2: Update Target Frameworks & Dependencies

**Priority:** HIGH | **Effort:** 1 day | **Impact:** Security, performance, long-term support

### Tasks

#### 2.1 Update Target Frameworks
- [ ] **Core library:** Decide on .NET Standard 2.1 vs .NET 8
  - Keep .NET Standard 2.1 if .NET Framework 4.7.2+ compatibility needed
  - Move to .NET 8 for modern features and performance
- [ ] **Windowing library:** Update from .NET 7.0 → .NET 8
- [ ] **Test projects:** Update to .NET 8
- [ ] Update all `.csproj` files

#### 2.2 Update Test Dependencies
- [ ] xUnit: 2.5.0 → latest (2.9.x)
- [ ] FluentAssertions: 6.11.0 → latest (6.12.x)
- [ ] Microsoft.NET.Test.Sdk: 17.6.3 → latest
- [ ] coverlet.collector: 6.0.0 → latest

#### 2.3 Enable Modern Language Features
- [ ] Update to C# 12 (if moving to .NET 8)
- [ ] Enable nullable reference types throughout:
  ```xml
  <Nullable>enable</Nullable>
  ```
- [ ] Use file-scoped namespaces:
  ```csharp
  namespace Gehtsoft.Xce.Conio;
  ```
- [ ] Update `.editorconfig` for C# 12 features

#### 2.4 Verify Compatibility
- [ ] Run all tests after framework update
- [ ] Check for breaking API changes
- [ ] Update documentation with new requirements

### Acceptance Criteria
- [ ] All projects target .NET 8 (or appropriate framework)
- [ ] All dependencies updated
- [ ] All tests pass
- [ ] No compilation warnings related to obsolete APIs
- [ ] Performance baseline established

### Benefits
- Access to latest performance improvements
- Security patches and bug fixes
- Modern language features (required patterns, collection expressions, etc.)
- Extended support timeline (.NET 8 LTS until Nov 2026)

---

## Phase 3: Expand Test Coverage (Critical Gap)

**Priority:** HIGH | **Effort:** 5-7 days | **Impact:** Quality assurance, regression prevention

**Current Coverage:** ~15% (only low-level utilities tested)
**Target Coverage:** 70%+

### 3.1 Window Management Tests (Priority 1)
**Effort:** 2 days

- [ ] Create `WindowTests.cs`
  - [ ] Test window creation and initialization
  - [ ] Test parent-child relationships
  - [ ] Test window hierarchy traversal
  - [ ] Test visibility changes
  - [ ] Test position and size management
  - [ ] Test invalidation/validation
  - [ ] Test coordinate transformations (screen ↔ window ↔ client)
  - [ ] Test bounds checking

- [ ] Create `WindowManagerTests.cs`
  - [ ] Test window registration/unregistration
  - [ ] Test Z-ordering (bring to front, send to back)
  - [ ] Test focus management
  - [ ] Test modal dialog stack
  - [ ] Test message pump basics
  - [ ] Test idle event firing
  - [ ] Test window enumeration

- [ ] Create `WindowBorderContainerTests.cs`
  - [ ] Test mouse-based move operations
  - [ ] Test mouse-based resize operations
  - [ ] Test title bar dragging
  - [ ] Test corner dragging
  - [ ] Test keyboard shortcuts (Ctrl-Space mode)

### 3.2 Input Handling Tests (Priority 1)
**Effort:** 2 days

- [ ] Create `KeyboardInputTests.cs`
  - [ ] Test key press/release events
  - [ ] Test modifier key tracking (Shift, Ctrl, Alt)
  - [ ] Test ScanCode mapping
  - [ ] Test character input
  - [ ] Test keyboard layout changes (if applicable)

- [ ] Create `MouseInputTests.cs`
  - [ ] Test mouse button events
  - [ ] Test mouse movement tracking
  - [ ] Test mouse wheel events
  - [ ] Test mouse capture/release
  - [ ] Test coordinate translation

- [ ] Create `InputListenerTests.cs`
  - [ ] Test listener registration/unregistration
  - [ ] Test event dispatching
  - [ ] Test multiple listeners
  - [ ] Test listener ordering

### 3.3 Output Rendering Tests (Priority 2)
**Effort:** 1-2 days

- [ ] Create `Win32ConsoleOutputTests.cs`
  - [ ] Test buffer write operations
  - [ ] Test cursor positioning
  - [ ] Test color rendering
  - [ ] Test screen buffer reading (if applicable)

- [ ] Create `VtConsoleOutputTests.cs`
  - [ ] Test VT sequence generation
  - [ ] Test 16-color palette rendering
  - [ ] Test differential rendering (change tracking)

- [ ] Create `VtTrueColorConsoleOutputTests.cs`
  - [ ] Test RGB color sequence generation
  - [ ] Test true color rendering
  - [ ] Test color conversion accuracy

- [ ] Create `CanvasRenderingTests.cs` (expand existing)
  - [ ] Test canvas composition (paint canvas to canvas)
  - [ ] Test clipping
  - [ ] Test color blending
  - [ ] Test style rendering

### 3.4 Dialog System Tests (Priority 2)
**Effort:** 2 days

- [ ] Create `DialogTests.cs`
  - [ ] Test modal dialog lifecycle
  - [ ] Test dialog result handling
  - [ ] Test focus management within dialog
  - [ ] Test Enter/Escape handling
  - [ ] Test Alt+hotkey navigation

- [ ] Create control-specific tests:
  - [ ] `DialogItemButtonTests.cs`
  - [ ] `DialogItemCheckBoxTests.cs`
  - [ ] `DialogItemRadioBoxTests.cs`
  - [ ] `DialogItemSingleLineTextTests.cs`
  - [ ] `DialogItemListBoxTests.cs`
  - [ ] `DialogItemComboBoxTests.cs`

- [ ] Create `TabNavigationTests.cs`
  - [ ] Test tab order
  - [ ] Test tab cycling
  - [ ] Test Shift+Tab (reverse navigation)

### 3.5 Cross-Platform Integration Tests (Priority 3)
**Effort:** 1 day

- [ ] Set up GitHub Actions CI/CD
  - [ ] Windows runner (latest)
  - [ ] Ubuntu runner (latest)
  - [ ] macOS runner (latest)

- [ ] Create integration test suite
  - [ ] Test platform-specific implementations
  - [ ] Test graceful fallbacks
  - [ ] Test VT support detection

- [ ] Add code coverage reporting
  - [ ] Integrate coverlet
  - [ ] Upload to CodeCov or similar
  - [ ] Set coverage thresholds

### Testing Approach

**Unit Tests:**
- Mock `IConsoleInput` and `IConsoleOutput` for isolation
- Use xUnit `[Theory]` for parameterized tests
- Use FluentAssertions for readable assertions

**Integration Tests:**
- Create test console application
- Automate input simulation
- Verify output buffer contents

**Example Test Structure:**
```csharp
public class WindowTests
{
    [Fact]
    public void Constructor_SetsPropertiesCorrectly()
    {
        // Arrange
        var output = new MockConsoleOutput();

        // Act
        var window = new Window(output, 10, 20, 80, 25);

        // Assert
        window.Left.Should().Be(10);
        window.Top.Should().Be(20);
        window.Width.Should().Be(80);
        window.Height.Should().Be(25);
    }

    [Fact]
    public void AddChild_AddsWindowToChildList()
    {
        // Arrange
        var output = new MockConsoleOutput();
        var parent = new Window(output, 0, 0, 100, 50);
        var child = new Window(output, 10, 10, 50, 25);

        // Act
        parent.AddChild(child);

        // Assert
        parent.Children.Should().Contain(child);
        child.Parent.Should().Be(parent);
    }
}
```

### Acceptance Criteria
- [ ] Code coverage reaches 70%+ overall
- [ ] All critical paths tested
- [ ] CI/CD pipeline runs tests on all platforms
- [ ] Test execution time < 30 seconds
- [ ] Zero test flakiness

### Benefits
- Prevents regressions during modernization
- Ensures cross-platform compatibility
- Improves code quality through test-driven refactoring
- Provides living documentation of expected behavior

---

## Phase 4: Improve Error Handling & Logging

**Priority:** MEDIUM | **Effort:** 3-4 days | **Impact:** Debuggability, production reliability

### Tasks

#### 4.1 Add Logging Infrastructure
- [ ] Add NuGet package: `Microsoft.Extensions.Logging.Abstractions`
- [ ] Create `ILogger` integration throughout library
- [ ] Add logger injection to key classes:
  - [ ] `ConioFactory`
  - [ ] `Win32ConsoleOutput`
  - [ ] `WindowManager`
  - [ ] `Dialog`

- [ ] Define logging levels:
  - **Trace:** Detailed internal operations
  - **Debug:** Platform detection, mode selection
  - **Information:** Window operations, focus changes
  - **Warning:** Fallback scenarios, deprecated features
  - **Error:** P/Invoke failures, invalid operations
  - **Critical:** Unrecoverable errors

#### 4.2 Enhance Error Handling
- [ ] **Win32 API Error Checking:**
  ```csharp
  private void SetConsoleWindowSize(int w, int h)
  {
      if (!Win32.SetConsoleWindowInfo(...))
      {
          var error = Marshal.GetLastWin32Error();
          _logger?.LogError("SetConsoleWindowInfo failed: {Error}", error);
          throw new Win32Exception(error);
      }
  }
  ```

- [ ] **Wrap P/Invoke calls** with try-catch and proper error messages
- [ ] **Add validation** in window hierarchy operations
- [ ] **Throw meaningful exceptions** instead of silent failures

#### 4.3 Add Null Checking
- [ ] Enable nullable reference types in all projects
- [ ] Add `ArgumentNullException.ThrowIfNull()` to public methods
- [ ] Update method signatures with `?` annotations
- [ ] Fix all nullable warnings

#### 4.4 Add Diagnostic Information
- [ ] Log platform detection results
- [ ] Log selected output mode (Win32/VT/Net)
- [ ] Log terminal capabilities
- [ ] Add version information logging

### Example Implementation

```csharp
public class Win32ConsoleOutput : IConsoleOutput
{
    private readonly ILogger<Win32ConsoleOutput>? _logger;

    public Win32ConsoleOutput(ILogger<Win32ConsoleOutput>? logger = null)
    {
        _logger = logger;
        _logger?.LogInformation("Initializing Win32 console output");
    }

    public void Paint(ICanvas canvas)
    {
        ArgumentNullException.ThrowIfNull(canvas);

        _logger?.LogTrace("Painting canvas: {Width}x{Height}",
            canvas.Width, canvas.Height);

        try
        {
            // ... painting logic ...
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to paint canvas");
            throw;
        }
    }
}
```

### Acceptance Criteria
- [ ] All P/Invoke calls have error checking
- [ ] All public methods validate parameters
- [ ] Logging available throughout library
- [ ] No nullable warnings
- [ ] Error messages are clear and actionable

### Benefits
- Easier debugging in production
- Better error messages for users
- Prevents null reference exceptions
- Improves code quality

---

## Phase 5: Simplify Color Scheme System

**Priority:** MEDIUM | **Effort:** 2 days | **Impact:** Build simplicity, maintainability

### Current Issue
- Uses XSLT transformation at build time to generate C# code from XML
- Adds build complexity and MSBuild dependency
- Hard to understand and maintain

### Options

#### Option A: C# Source Generators (Modern)
- Replace XSLT with incremental source generator
- Better IDE integration (IntelliSense, debugging)
- Compile-time safety

**Pros:** Modern, type-safe, good tooling support
**Cons:** Moderate complexity to implement

#### Option B: Runtime JSON Configuration
- Ship color schemes as JSON files
- Load at runtime with System.Text.Json
- Easier customization by users

**Pros:** Simple, flexible, user-customizable
**Cons:** Runtime overhead, validation needed

#### Option C: Code-Based Schemes (Recommended)
- Define color schemes directly in C# code
- Simplest approach, no external files
- Easier to maintain

**Pros:** Simplest, no external dependencies, strongly typed
**Cons:** Less flexible for users to customize

### Recommended: Option C

#### Tasks
- [ ] Create `ColorSchemes.cs` with static scheme definitions
- [ ] Port existing color scheme definitions from XML
- [ ] Remove XSLT build step from project files
- [ ] Delete XML and XSLT files
- [ ] Update API to use new color scheme system
- [ ] Add documentation for creating custom schemes

#### Example Implementation

```csharp
public static class ColorSchemes
{
    public static readonly ColorScheme Default = new()
    {
        WindowBackground = CanvasColor.Palette(0, 7), // Black on white
        WindowForeground = CanvasColor.Palette(7, 0), // White on black
        ButtonNormal = CanvasColor.Palette(0, 3),     // Black on cyan
        ButtonFocused = CanvasColor.Palette(15, 1),   // White on blue
        // ... more definitions ...
    };

    public static readonly ColorScheme Dark = new()
    {
        // Dark theme definitions...
    };

    // Allow registration of custom schemes
    private static Dictionary<string, ColorScheme> _customSchemes = new();

    public static void Register(string name, ColorScheme scheme)
    {
        _customSchemes[name] = scheme;
    }
}
```

### Acceptance Criteria
- [ ] No XSLT build step
- [ ] All existing color schemes ported
- [ ] API backwards compatible (or migration path provided)
- [ ] Documentation updated
- [ ] Custom scheme registration possible

### Benefits
- Removes build complexity
- Easier to understand and maintain
- No MSBuild dependencies
- Better debugging experience

---

## Phase 6: Add Comprehensive Documentation

**Priority:** MEDIUM | **Effort:** 3-4 days | **Impact:** Developer experience, adoption

### Deliverables

#### 6.1 README.md
- [ ] Project overview and purpose
- [ ] Key features list
- [ ] Platform support matrix (Windows/Linux/macOS)
- [ ] Quick start code examples
- [ ] Installation instructions (NuGet)
- [ ] Architecture overview (high-level)
- [ ] Links to detailed documentation
- [ ] License information
- [ ] Contributing guidelines

#### 6.2 API Documentation
- [ ] Add XML documentation comments to all public types
- [ ] Add XML documentation comments to all public members
- [ ] Include `<example>` tags with code samples
- [ ] Document exceptions thrown
- [ ] Document thread safety
- [ ] Generate DocFX documentation site (optional)

#### 6.3 Architecture Documentation
- [ ] Create `ARCHITECTURE.md`
- [ ] Component diagram (Input, Output, Canvas, Windowing)
- [ ] Sequence diagrams for key operations:
  - Event flow (keyboard/mouse → window)
  - Rendering pipeline (canvas → screen)
  - Modal dialog lifecycle
- [ ] Design decisions and rationale
- [ ] Extension points

#### 6.4 Migration Guide
- [ ] `MIGRATION.md` for updating from older versions
- [ ] Breaking changes documentation
- [ ] ConEmu removal guide (after Phase 1)
- [ ] Best practices and patterns
- [ ] Common pitfalls and solutions

#### 6.5 Examples
Create example projects demonstrating:
- [ ] Hello World console app
- [ ] Simple windowed application
- [ ] Dialog with various controls
- [ ] Menu system
- [ ] Custom canvas painting
- [ ] Color scheme customization

### Example XML Documentation

```csharp
/// <summary>
/// Represents a window in the console UI system.
/// </summary>
/// <remarks>
/// Windows can be hierarchical (parent-child relationships), support
/// focus management, and can be modal or non-modal. All coordinates
/// are relative to the parent window or screen.
/// </remarks>
/// <example>
/// <code>
/// var window = new Window(output, 10, 10, 80, 25);
/// window.AddChild(new Dialog(...));
/// windowManager.ShowWindow(window);
/// </code>
/// </example>
public class Window : IConsoleInputListener
{
    /// <summary>
    /// Gets or sets the left position of the window relative to its parent.
    /// </summary>
    /// <value>The X coordinate in characters.</value>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when value is negative.
    /// </exception>
    public int Left { get; set; }
}
```

### Acceptance Criteria
- [ ] README.md is clear and comprehensive
- [ ] All public APIs have XML documentation
- [ ] Examples compile and run
- [ ] Architecture document explains design
- [ ] Migration guide addresses breaking changes

### Benefits
- Lowers barrier to entry for new users
- Reduces support burden
- Demonstrates professionalism
- Improves maintainability

---

## Phase 7: Code Quality Improvements

**Priority:** LOW-MEDIUM | **Effort:** 4-5 days | **Impact:** Maintainability

### Tasks

#### 7.1 Address SonarQube Issues (414 reported)
- [ ] Fix high-severity issues first
- [ ] Reduce cyclomatic complexity (refactor complex methods)
- [ ] Remove code duplication (extract common code)
- [ ] Fix potential null reference exceptions
- [ ] Address performance warnings
- [ ] Re-run SonarQube analysis

#### 7.2 Eliminate Magic Numbers
- [ ] Identify all magic numbers in codebase
- [ ] Replace with named constants or enums
- [ ] Example:
  ```csharp
  // Before:
  if ((style & 0x80) != 0) ...

  // After:
  const int STYLE_BOLD = 0x80;
  if ((style & STYLE_BOLD) != 0) ...
  ```

#### 7.3 Improve Resource Management
- [ ] Audit all `IDisposable` implementations
- [ ] Ensure proper disposal patterns
- [ ] Add `using` statements consistently
- [ ] Check memory-mapped file cleanup
- [ ] Add finalizers where needed (with suppress warning)

#### 7.4 Code Modernization
- [ ] Replace custom collection code with LINQ where appropriate
- [ ] Use collection expressions (C# 12):
  ```csharp
  // Before:
  var list = new List<int> { 1, 2, 3 };

  // After:
  var list = [1, 2, 3];
  ```
- [ ] Use `Span<T>` and `Memory<T>` for buffer operations
- [ ] Replace `CharInfoArray`/`IntArray` with span-based APIs
- [ ] Update to pattern matching where beneficial:
  ```csharp
  // Before:
  if (obj is Window)
  {
      var window = (Window)obj;
      // use window
  }

  // After:
  if (obj is Window window)
  {
      // use window
  }
  ```

#### 7.5 Consider Coding Convention Updates (Optional)
- [ ] Discuss with team: remove Hungarian notation (`m` prefix)?
- [ ] Update `.editorconfig` if consensus reached
- [ ] Apply code cleanup across codebase
- [ ] Run automated refactoring tools

### Acceptance Criteria
- [ ] SonarQube issues reduced by 80%+
- [ ] No magic numbers in code
- [ ] All resources properly disposed
- [ ] Code passes static analysis
- [ ] Consistent code style throughout

### Benefits
- Easier to maintain and understand
- Fewer bugs
- Better performance
- Modern C# idioms

---

## Phase 8: Performance Optimization

**Priority:** LOW | **Effort:** 3-4 days | **Impact:** Responsiveness

### Approach
1. Profile first, optimize second
2. Establish baseline performance metrics
3. Identify bottlenecks through profiling
4. Apply targeted optimizations
5. Measure improvements

### Profiling Targets
- [ ] Canvas rendering pipeline
- [ ] Window repainting (differential updates)
- [ ] Event dispatching in WindowManager
- [ ] Coordinate transformation calculations
- [ ] VT sequence generation

### Optimization Opportunities

#### 8.1 Use Span<T> for Buffer Operations
- [ ] Replace `CharInfoArray` with `Span<CharInfo>`
- [ ] Use `stackalloc` for small buffers
- [ ] Avoid allocations in hot paths

#### 8.2 Object Pooling
- [ ] Pool `StringBuilder` instances
- [ ] Pool canvas objects for rendering
- [ ] Pool event objects

#### 8.3 Optimize VT Sequence Generation
- [ ] Cache common sequences
- [ ] Batch color changes
- [ ] Minimize sequence length

#### 8.4 Reduce Boxing
- [ ] Identify boxing in color conversions
- [ ] Use generic constraints to avoid boxing
- [ ] Use `ref struct` where appropriate

#### 8.5 Cache Calculations
- [ ] Cache coordinate transformations
- [ ] Cache color conversions (palette ↔ RGB)
- [ ] Memoize expensive computations

### Performance Targets
- Canvas paint: < 16ms (60 FPS)
- Event dispatch: < 1ms
- Window layout: < 5ms

### Acceptance Criteria
- [ ] Baseline performance metrics documented
- [ ] Target optimizations identified through profiling
- [ ] Measurable improvements achieved
- [ ] No regressions in functionality

### Benefits
- More responsive UI
- Better user experience
- Reduced CPU usage

---

## Phase 9: Consider Accessibility Features

**Priority:** LOW | **Effort:** Variable | **Impact:** Accessibility

### Potential Additions
- [ ] Screen reader support (Windows Narrator integration)
- [ ] High contrast theme support
- [ ] Keyboard-only navigation enhancements
- [ ] Focus indicators (visual and auditory)
- [ ] Accessible name/description for controls
- [ ] ARIA-like role attributes

### Research Required
- Investigate console accessibility APIs
- Determine feasibility of screen reader integration
- Survey user needs

### Acceptance Criteria
- [ ] Basic screen reader support on Windows
- [ ] High contrast themes work correctly
- [ ] Keyboard navigation is comprehensive

### Benefits
- Wider audience reach
- Compliance with accessibility standards
- Better user experience for all

---

## Phase 10: Evaluate Competitive Landscape (STRATEGIC)

**Priority:** HIGH (but should be done FIRST - see Phase 0)
**Effort:** 2-3 days
**Impact:** Strategic direction

### Modern Alternatives to Evaluate

#### Terminal.Gui
- **Repository:** https://github.com/gui-cs/Terminal.Gui
- **NuGet:** Terminal.Gui
- **License:** MIT
- **Stars:** 9k+
- **Status:** Active development

**Features to Verify:**
- [ ] Multi-platform support (Windows, Linux, macOS)
- [ ] Rich control library
- [ ] Event-driven architecture
- [ ] Keyboard and mouse support
- [ ] Modal dialogs
- [ ] Theming/color schemes
- [ ] Unicode support
- [ ] Documentation quality

#### Spectre.Console
- **Repository:** https://github.com/spectreconsole/spectre.console
- **NuGet:** Spectre.Console
- **License:** MIT
- **Stars:** 8k+

**Features to Verify:**
- [ ] Rich formatting
- [ ] Tables, trees, progress bars
- [ ] Prompts and dialogs
- [ ] Color support
- [ ] Cross-platform

#### gui.cs (older, may be superseded by Terminal.Gui)

### Questions to Answer
1. Does this library offer unique capabilities not found in alternatives?
2. Is continued maintenance justified given effort vs. alternatives?
3. Should it focus on a specific niche?
4. Could features be contributed to existing projects?
5. What is the migration effort to alternatives?
6. What existing codebases depend on this library?

### Deliverable: Strategic Recommendation
Create document answering:
- **Build:** Modernize existing library (with justification)
- **Buy:** Migrate to Terminal.Gui or alternative (with migration plan)
- **Hybrid:** Use alternative for new features, maintain legacy for compatibility

---

## Execution Timeline

### Recommended Order (UPDATED)

#### Week 0: Strategic Decision
🎯 **Phase 0** - Evaluate Terminal.Gui and alternatives
- Research features and capabilities
- Create feature comparison matrix
- Assess migration complexity
- Make build vs. buy decision

**Decision Point:** Proceed with modernization OR plan migration

---

### If Proceeding with Modernization:

#### Week 1: Foundation
✅ **Phase 1** - Remove ConEmu, modernize platform detection (2-3 days)
✅ **Phase 2** - Update frameworks to .NET 8 (1 day)

**Outcome:** Modernized, simplified codebase

#### Week 2: Quality Assurance
✅ **Phase 3** - Expand test coverage (5-7 days)

**Outcome:** Robust, testable library

#### Week 3: Documentation & Polish
✅ **Phase 4** - Improve error handling & logging (3-4 days)
✅ **Phase 5** - Simplify color scheme system (2 days)

**Outcome:** Professional, maintainable library

#### Week 4: Refinement
✅ **Phase 6** - Add comprehensive documentation (3-4 days)
✅ **Phase 7** - Code quality improvements (start, continue as needed)

**Outcome:** Well-documented, high-quality library

---

### Future Iterations (if modernizing):
- **Phase 8** - Performance optimization (profile-guided)
- **Phase 9** - Accessibility features (if needed)

---

## Risk Assessment

### Breaking Changes

1. **ConEmu Removal**
   - **Impact:** Any code explicitly using `ConEmuConsoleOutput` will break
   - **Mitigation:**
     - Mark as obsolete in minor version
     - Provide migration guide
     - Remove in next major version

2. **Framework Update (.NET 7 → .NET 8)**
   - **Impact:** Must recompile dependent projects
   - **Mitigation:** Low risk, mostly compatible; test thoroughly

3. **Nullable Reference Types**
   - **Impact:** May require changes in consuming code
   - **Mitigation:** Warnings only, not breaking

### Compatibility Concerns

1. **Multi-Targeting**
   - Removing .NET Standard 2.1 would break .NET Framework 4.7.2+ compatibility
   - **Recommendation:** Keep multi-targeting if needed by consumers

2. **API Changes**
   - Any public API changes must be carefully considered
   - **Recommendation:** Semantic versioning, deprecation warnings

---

## Success Metrics

### Code Quality
- [ ] Test coverage > 70%
- [ ] SonarQube issues < 50
- [ ] Zero critical bugs
- [ ] Build warnings = 0

### Documentation
- [ ] All public APIs documented
- [ ] README with examples
- [ ] Architecture documentation complete
- [ ] Migration guide available

### Performance
- [ ] Canvas paint < 16ms (60 FPS)
- [ ] Event dispatch < 1ms
- [ ] Window layout < 5ms
- [ ] No memory leaks

### Maintainability
- [ ] Lines of code reduced by 10%+
- [ ] Cyclomatic complexity < 15 per method
- [ ] No code duplication > 5%
- [ ] Modern C# idioms throughout

---

## Next Steps

1. **IMMEDIATE:** Complete Phase 0 evaluation of Terminal.Gui
2. **Decision:** Build vs. Buy (based on evaluation)
3. **If Build:** Follow phases 1-7 over 3-4 weeks
4. **If Buy:** Create detailed migration plan

---

## Questions for Stakeholders

1. Are there any .NET Framework 4.x projects depending on this library?
2. What is the appetite for breaking changes?
3. Is there budget/time for 3-4 weeks of modernization work?
4. Are there unique requirements not met by Terminal.Gui?
5. What existing codebases would be affected?

---

**Document Owner:** Development Team
**Last Updated:** 2025-11-10
**Status:** Proposed - Awaiting Phase 0 Evaluation
