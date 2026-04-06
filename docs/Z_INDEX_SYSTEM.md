# Z-Index System Documentation

## Overview
This document outlines the standardized z-index system implemented across the ecommerce Angular application to ensure proper layering of UI elements and prevent conflicts.

## Z-Index Variables
All z-index values are defined as CSS custom properties in `styles.scss`:

```scss
:root {
  /* Z-Index System - Standardized Layering */
  --z-background: 1;        /* Background overlays, hero sections */
  --z-content: 2;           /* Content overlays, carousel elements */
  --z-sticky: 10;           /* Sticky headers, footers, sticky elements */
  --z-dropdown: 1000;       /* Dropdown menus, user menus */
  --z-navbar: 1010;         /* Navigation bars (higher than sticky elements) */
  --z-modal-backdrop: 1040; /* Modal backdrops */
  --z-modal: 1050;          /* Modal dialogs */
  --z-notification: 9999;   /* Notifications, loaders, alerts */
  --z-toast: 99999;         /* Toast notifications (highest priority) */

  /* Layout Dimensions */
  --navbar-height: 64px;    /* Height of the admin navbar */
}
```

## Usage Guidelines

### 1. Background Elements (z-index: 1)
- Hero section backgrounds
- Image overlays
- Background decorative elements

**Example:**
```scss
.hero-background {
  z-index: var(--z-background);
}
```

### 2. Content Elements (z-index: 2)
- Content overlays
- Carousel overlays
- Text content over backgrounds

**Example:**
```scss
.content-overlay {
  z-index: var(--z-content);
}
```

### 3. Sticky Elements (z-index: 10)
- Sticky table headers
- Sticky navigation elements
- Sticky footers
- Sticky sidebars

**Example:**
```scss
.sticky-header {
  position: sticky;
  top: var(--navbar-height); /* Account for navbar height */
  z-index: var(--z-sticky);
}
```

**Important:** Sticky table headers should use `top: var(--navbar-height)` to prevent overlap with the admin navbar.

### 4. Dropdown/Navigation (z-index: 1000)
- User dropdown menus
- Navigation dropdowns
- Context menus

**Example:**
```scss
.dropdown-menu {
  z-index: var(--z-dropdown);
}
```

### 5. Modal System (z-index: 1040-1050)
- Modal backdrops: `var(--z-modal-backdrop)`
- Modal dialogs: `var(--z-modal)`

**Example:**
```scss
.modal-backdrop {
  z-index: var(--z-modal-backdrop);
}

.modal {
  z-index: var(--z-modal);
}
```

### 6. Notifications (z-index: 9999)
- Loading spinners
- Alert notifications
- Rate limit notifications
- Logout confirmations

**Example:**
```scss
.loader {
  z-index: var(--z-notification);
}
```

### 7. Toast Notifications (z-index: 99999)
- Toast messages (highest priority)
- System-wide notifications

**Example:**
```scss
.toast-container {
  z-index: var(--z-toast);
}
```

## Components Updated

### ✅ Completed Updates

1. **Global Styles** (`styles.scss`)
   - Added standardized z-index variables
   - Updated toast and loader z-index values

2. **Admin Components**
   - `admin-users.component.scss` - Modal and sticky header z-index
   - `admin-orders.component.scss` - Sticky header and notification z-index
   - `admin-products.component.scss` - Sticky header z-index
   - `admin-navbar.component.scss` - Navigation z-index
   - `admin-footer.component.scss` - Footer z-index

3. **User Interface Components**
   - `navbar.component.scss` - Dropdown menu z-index
   - `login.component.scss` - Background and content z-index
   - `register.component.scss` - Background and content z-index
   - `products.component.scss` - Carousel and overlay z-index
   - `home.component.scss` - Hero section z-index
   - `wishlist.component.scss` - Content overlay z-index

4. **Modal Components**
   - `cart.component.scss` - Sticky elements and checkout modal
   - `product-detail.component.scss` - Product detail modal
   - `checkout.component.scss` - Checkout modal

5. **Shared Components**
   - `logout-confirmation.component.scss` - Notification z-index
   - `loader.component.scss` - Loader z-index
   - `rate-limit-notification.component.ts` - Notification z-index

## Best Practices

### 1. Always Use CSS Variables
```scss
// ✅ Good
z-index: var(--z-modal);

// ❌ Bad
z-index: 1050;
```

### 2. Use Calculated Values for Layering
```scss
// For elements that need to be slightly above content
z-index: calc(var(--z-content) + 1);

// For elements that need to be slightly above sticky
z-index: calc(var(--z-sticky) + 1);
```

### 3. Document Custom Z-Index Values
If you need a custom z-index value that doesn't fit the standard system, document it and consider adding it to the global variables.

### 4. Test Modal Stacking
When implementing multiple modals or overlays, test that they stack correctly and don't interfere with each other.

## Common Issues and Solutions

### Issue: Modal appears behind other elements
**Solution:** Ensure modal uses `var(--z-modal)` and backdrop uses `var(--z-modal-backdrop)`

### Issue: Dropdown menu appears behind content
**Solution:** Use `var(--z-dropdown)` for dropdown menus

### Issue: Sticky header not staying on top
**Solution:** Use `var(--z-sticky)` for sticky elements

### Issue: Sticky table header overlaps with navbar
**Solution:** Use `top: var(--navbar-height)` instead of `top: 0` for sticky table headers

### Issue: Toast notifications not visible
**Solution:** Ensure toasts use `var(--z-toast)` (highest priority)

## Testing Checklist

- [ ] All modals appear above other content
- [ ] Dropdown menus appear above page content
- [ ] Sticky headers stay on top when scrolling
- [ ] Toast notifications appear above all other elements
- [ ] Multiple modals stack correctly
- [ ] Mobile responsive z-index behavior works correctly

## Future Considerations

1. **Dynamic Z-Index Management**: Consider implementing a service for dynamic z-index management in complex scenarios
2. **Animation Z-Index**: Add specific z-index values for animated elements
3. **Accessibility**: Ensure z-index doesn't interfere with screen reader navigation
4. **Performance**: Monitor for excessive z-index stacking that might impact performance

## Maintenance

- Review z-index usage during code reviews
- Update this documentation when adding new z-index values
- Test z-index behavior across different browsers and devices
- Consider using CSS layers (when widely supported) for more sophisticated layering
