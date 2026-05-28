---
name: Insightful Learning
colors:
  surface: '#f7f9fb'
  surface-dim: '#d8dadc'
  surface-bright: '#f7f9fb'
  surface-container-lowest: '#ffffff'
  surface-container-low: '#f2f4f6'
  surface-container: '#eceef0'
  surface-container-high: '#e6e8ea'
  surface-container-highest: '#e0e3e5'
  on-surface: '#191c1e'
  on-surface-variant: '#464555'
  inverse-surface: '#2d3133'
  inverse-on-surface: '#eff1f3'
  outline: '#777587'
  outline-variant: '#c7c4d8'
  surface-tint: '#4d44e3'
  primary: '#3525cd'
  on-primary: '#ffffff'
  primary-container: '#4f46e5'
  on-primary-container: '#dad7ff'
  inverse-primary: '#c3c0ff'
  secondary: '#505f76'
  on-secondary: '#ffffff'
  secondary-container: '#d0e1fb'
  on-secondary-container: '#54647a'
  tertiary: '#7e3000'
  on-tertiary: '#ffffff'
  tertiary-container: '#a44100'
  on-tertiary-container: '#ffd2be'
  error: '#ba1a1a'
  on-error: '#ffffff'
  error-container: '#ffdad6'
  on-error-container: '#93000a'
  primary-fixed: '#e2dfff'
  primary-fixed-dim: '#c3c0ff'
  on-primary-fixed: '#0f0069'
  on-primary-fixed-variant: '#3323cc'
  secondary-fixed: '#d3e4fe'
  secondary-fixed-dim: '#b7c8e1'
  on-secondary-fixed: '#0b1c30'
  on-secondary-fixed-variant: '#38485d'
  tertiary-fixed: '#ffdbcc'
  tertiary-fixed-dim: '#ffb695'
  on-tertiary-fixed: '#351000'
  on-tertiary-fixed-variant: '#7b2f00'
  background: '#f7f9fb'
  on-background: '#191c1e'
  surface-variant: '#e0e3e5'
typography:
  headline-xl:
    fontFamily: Be Vietnam Pro
    fontSize: 40px
    fontWeight: '700'
    lineHeight: '1.2'
    letterSpacing: -0.02em
  headline-lg:
    fontFamily: Be Vietnam Pro
    fontSize: 32px
    fontWeight: '600'
    lineHeight: '1.25'
    letterSpacing: -0.01em
  headline-md:
    fontFamily: Be Vietnam Pro
    fontSize: 24px
    fontWeight: '600'
    lineHeight: '1.3'
  headline-sm:
    fontFamily: Be Vietnam Pro
    fontSize: 20px
    fontWeight: '600'
    lineHeight: '1.4'
  body-lg:
    fontFamily: Inter
    fontSize: 18px
    fontWeight: '400'
    lineHeight: '1.6'
  body-md:
    fontFamily: Inter
    fontSize: 16px
    fontWeight: '400'
    lineHeight: '1.5'
  body-sm:
    fontFamily: Inter
    fontSize: 14px
    fontWeight: '400'
    lineHeight: '1.5'
  label-caps:
    fontFamily: Inter
    fontSize: 12px
    fontWeight: '600'
    lineHeight: '1.2'
    letterSpacing: 0.05em
  label-sm:
    fontFamily: Inter
    fontSize: 12px
    fontWeight: '500'
    lineHeight: '1.2'
  headline-lg-mobile:
    fontFamily: Be Vietnam Pro
    fontSize: 28px
    fontWeight: '600'
    lineHeight: '1.3'
rounded:
  sm: 0.25rem
  DEFAULT: 0.5rem
  md: 0.75rem
  lg: 1rem
  xl: 1.5rem
  full: 9999px
spacing:
  unit: 8px
  container-max-width: 1440px
  gutter: 24px
  margin-mobile: 16px
  margin-desktop: 40px
  stack-sm: 8px
  stack-md: 16px
  stack-lg: 32px
---

## Brand & Style

The design system is engineered for an educational SaaS environment that prioritizes clarity, intellectual focus, and user confidence. The brand personality is that of a "Sophisticated Mentor"—highly capable and technical, yet accessible and encouraging. 

The aesthetic follows a **Modern Corporate** direction infused with **Minimalist** principles. It utilizes significant whitespace to reduce cognitive load during complex learning tasks, while maintaining a structured, card-based interface that organizes AI-generated insights into digestible modules. The emotional response should be one of "calm productivity"—users should feel they are in a high-quality, stable environment where technology serves pedagogy.

## Colors

The palette is anchored by a deep, authoritative Indigo primary color that signifies intelligence and trust. This is balanced by a Slate Grey secondary color used for utilitarian elements and supporting text to ensure the UI does not feel overwhelming.

The background uses a specific off-white (#F8FAFC) to distinguish itself from the pure white (#FFFFFF) of the card-based UI elements, creating a natural depth without the need for heavy borders. Accent colors for success and error states are slightly desaturated to maintain the professional, understated tone of the design system.

## Typography

This design system employs a dual-font strategy to optimize for both personality and legibility. **Be Vietnam Pro** is used for all headlines; its contemporary geometry and excellent Vietnamese character support provide a friendly yet professional editorial feel. 

**Inter** is utilized for body text, UI labels, and data-heavy components. Inter's tall x-height and neutral tone ensure that long-form educational content and AI chat logs remain highly readable over extended sessions. Hierarchy is established through generous scale differences and the strategic use of weights (600 for headers, 400 for content).

## Layout & Spacing

The layout philosophy is based on a **fixed-fluid hybrid grid**. On desktop, the main content area is capped at 1440px and centered to prevent line-lengths from becoming unreadable on ultra-wide monitors. 

We utilize a 12-column grid system. In the dashboard view, the sidebar occupies 2 or 3 columns, with the main workspace spanning the remainder. Spacing follows a strict 8px base unit. Vertical rhythm is maintained by using `stack-lg` (32px) between major card sections and `stack-md` (16px) for internal card padding. On mobile, margins shrink to 16px, and the 12-column grid collapses into a single-column stack.

## Elevation & Depth

Visual hierarchy is primarily achieved through **Tonal Layering** and **Ambient Shadows**. The base background layer (#F8FAFC) serves as the "floor." Interactive containers and educational cards are placed on the "surface" layer (#FFFFFF).

To communicate interactiveness, cards use a very soft, diffused shadow (Hex: #4F46E5 at 4% opacity, 12px blur, 4px Y-offset). This subtle indigo-tinted shadow reinforces the primary brand color even in the elevation system. Modals and dropdowns use a more pronounced elevation to appear closer to the user, effectively dimming the background with a light slate overlay.

## Shapes

The design system uses a **Rounded** shape language to evoke a welcoming and modern educational feel. The standard radius for UI components like buttons and small inputs is 0.5rem (8px). 

Main dashboard cards and large containers use `rounded-lg` (1.0rem / 16px) to create a distinct, soft-edged look that feels approachable and reduces the "clinical" feel often found in enterprise software. Interactive elements like tags or "AI status" chips may use `rounded-full` (pill-shaped) to distinguish them from structural layout blocks.

## Components

### Buttons
Primary buttons use a solid Indigo (#4F46E5) fill with white text. Secondary buttons use a subtle Slate-tinted ghost style with a 1px border (#E2E8F0). All buttons feature 8px rounded corners and a slight scale-down effect on click (98%) to provide tactile feedback.

### Cards
Cards are the core of the dashboard. They must have a white background, 16px corner radius, and the standard ambient indigo shadow. Inner padding should be a consistent 24px (stack-md + 8px) to maintain the "spacious" requirement.

### Input Fields
Inputs use Inter 16px for text to prevent auto-zooming on mobile iOS devices. Borders are Slate-200 (#E2E8F0), which transition to a 2px Indigo border on focus. Labels sit 8px above the field in `label-sm` style.

### AI Chat Interface
Chat bubbles for the AI should use the Primary color for the user and a soft Slate-50 background for the AI response to differentiate sources clearly. Use 12px padding and 12px rounded corners for the bubbles.

### Progress Indicators
Educational tracking uses thin, 4px height progress bars with the Primary Indigo color for progress and Slate-100 for the track, reflecting a precise and clean data visualization style.