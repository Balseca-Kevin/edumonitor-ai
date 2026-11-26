
import '@testing-library/jest-dom/vitest'

// Radix (panel «Período» y tooltips) y Recharts miden con ResizeObserver, que jsdom no trae.
globalThis.ResizeObserver ??= class {
  observe() {}
  unobserve() {}
  disconnect() {}
}
