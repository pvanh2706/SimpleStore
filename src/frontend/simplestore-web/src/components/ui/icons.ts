/** Stroke icons copied from the approved Sales visual reference (24×24, stroke-width 2). */
function circle(cx: number, cy: number, r: number): string {
  return `M${cx - r} ${cy}a${r} ${r} 0 1 0 ${2 * r} 0a${r} ${r} 0 1 0 ${-2 * r} 0`
}

function rect(x: number, y: number, width: number, height: number, r: number): string {
  const w = width - 2 * r
  const h = height - 2 * r
  return `M${x + r} ${y}h${w}a${r} ${r} 0 0 1 ${r} ${r}v${h}a${r} ${r} 0 0 1 ${-r} ${r}h${-w}a${r} ${r} 0 0 1 ${-r} ${-r}v${-h}a${r} ${r} 0 0 1 ${r} ${-r}Z`
}

function ellipse(cx: number, cy: number, rx: number, ry: number): string {
  return `M${cx - rx} ${cy}a${rx} ${ry} 0 1 0 ${2 * rx} 0a${rx} ${ry} 0 1 0 ${-2 * rx} 0`
}

export const icons = {
  brand: ['M7 8h10l-1 10H8L7 8Z', 'M9 8V6a3 3 0 0 1 6 0v2', 'M12 11v3', 'M10.5 12.5h3'],
  home: ['m3 11 9-7 9 7', 'M9 21V9h6v12'],
  navCart: [circle(9, 20, 1.2), circle(18, 20, 1.2), 'M3 4h2l2.6 10.4a1 1 0 0 0 .97.76H18a1 1 0 0 0 .98-.8L20.4 7H7.1'],
  purchase: ['M3 7h13v13H3z', 'M16 11h5v10h-5z', 'M5 5V3', 'M14 5V3'],
  product: ['M4 7a2 2 0 0 1 2-2h12a2 2 0 0 1 2 2v10a2 2 0 0 1-2 2H6a2 2 0 0 1-2-2Z', 'M8 5v14', 'M12 9h5', 'M12 13h5'],
  inventory: ['M3 7h18v10H3z', 'M7 7V5', 'M17 7V5', 'M7 12h4'],
  customer: ['M5 18a4 4 0 0 1 4-4h6a4 4 0 0 1 4 4', circle(12, 8, 4)],
  report: ['M3 7a2 2 0 0 1 2-2h14v14H5a2 2 0 0 1-2-2Z', 'M8 11h8', 'M8 15h5'],
  settings: [circle(12, 12, 3), 'M19.4 15a1.65 1.65 0 0 0 .33 1.82l.06.06a2 2 0 0 1 0 2.83 2 2 0 0 1-2.83 0l-.06-.06A1.65 1.65 0 0 0 15 19.4a1.65 1.65 0 0 0-1 .6 1.65 1.65 0 0 0-.33 1V21a2 2 0 0 1-4 0v-.09a1.65 1.65 0 0 0-.33-1 1.65 1.65 0 0 0-1-.6 1.65 1.65 0 0 0-1.82.33l-.06.06a2 2 0 0 1-2.83 0 2 2 0 0 1 0-2.83l.06-.06A1.65 1.65 0 0 0 4.6 15a1.65 1.65 0 0 0-.6-1 1.65 1.65 0 0 0-1-.33H3a2 2 0 0 1 0-4h.09a1.65 1.65 0 0 0 1-.33 1.65 1.65 0 0 0 .6-1 1.65 1.65 0 0 0-.33-1.82l-.06-.06a2 2 0 0 1 0-2.83 2 2 0 0 1 2.83 0l.06.06A1.65 1.65 0 0 0 9 4.6a1.65 1.65 0 0 0 1-.6 1.65 1.65 0 0 0 .33-1V3a2 2 0 0 1 4 0v.09a1.65 1.65 0 0 0 .33 1 1.65 1.65 0 0 0 1 .6 1.65 1.65 0 0 0 1.82-.33l.06-.06a2 2 0 0 1 2.83 0 2 2 0 0 1 0 2.83l-.06.06A1.65 1.65 0 0 0 19.4 9c.24.3.44.64.6 1 .1.31.34.58.67.74.3.15.64.24 1 .26H21a2 2 0 0 1 0 4h-.09a1.65 1.65 0 0 0-1 .33 1.65 1.65 0 0 0-.6 1Z'],
  collapse: ['m15 18-6-6 6-6', 'M9 12h12'],
  expand: ['m9 18 6-6-6-6', 'M15 12H3'],
  chevron: ['m6 9 6 6 6-6'],
  help: [circle(12, 12, 9), 'M9.1 9a3 3 0 1 1 5.8 1c0 2-3 2-3 4', 'M12 17h.01'],
  calendar: ['M8 2v4', 'M16 2v4', rect(3, 4, 18, 18, 2), 'M3 10h18'],
  calendarCheck: ['M8 2v4', 'M16 2v4', rect(3, 4, 18, 18, 2), 'M3 10h18', 'm9 16 2 2 4-4'],
  cart: [circle(8, 19, 1), circle(17, 19, 1), 'M3 4h2l2.2 8.5a1 1 0 0 0 .97.75H18a1 1 0 0 0 .98-.8L20 7H7.1'],
  bag: ['M5 6h14l-1 13H6L5 6Z', 'M9 6V4h6v2'],
  box: ['M4 7h16v10H4z', 'M8 4v3', 'M16 4v3'],
  kebab: [circle(12, 5, 1), circle(12, 12, 1), circle(12, 19, 1)],
  plus: ['M12 5v14', 'M5 12h14'],
  clock: ['M12 8v4l3 3', circle(12, 12, 9)],
  search: [circle(11, 11, 7), 'm20 20-3.5-3.5'],
  barcode: ['M4 7v10', 'M7 7v10', 'M10 7v10', 'M14 7v10', 'M17 7v10', 'M20 7v10'],
  scan: ['M7 4H5a1 1 0 0 0-1 1v2', 'M17 4h2a1 1 0 0 1 1 1v2', 'M7 20H5a1 1 0 0 1-1-1v-2', 'M17 20h2a1 1 0 0 0 1-1v-2', 'M8 12h8'],
  trash: ['M3 6h18', 'M8 6V4h8v2', 'M6 6l1 14h10l1-14', 'M10 10v6', 'M14 10v6'],
  trashMini: ['M3 6h18', 'M8 6V4h8v2', 'M6 6l1 14h10l1-14'],
  tag: ['M20.6 13.4 13.4 20.6a2 2 0 0 1-2.8 0L3 13V3h10l7.6 7.6a2 2 0 0 1 0 2.8Z', circle(7.5, 7.5, 1)],
  cash: [rect(3, 6, 18, 12, 2), 'M3 10h18'],
  bank: ['M4 20h16', 'M5 10h14', 'm12 3 8 5H4l8-5Z'],
  debt: [circle(9, 8, 3), 'M3 20a6 6 0 0 1 12 0', 'M17 11h4', 'M19 9v4'],
  check: ['m5 12 4 4L19 6'],
  warning: ['M10.3 3.9 1.8 18a2 2 0 0 0 1.7 3h17a2 2 0 0 0 1.7-3L13.7 3.9a2 2 0 0 0-3.4 0Z', 'M12 9v4', 'M12 17h.01'],
  print: ['M6 9V2h12v7', 'M6 18H4a2 2 0 0 1-2-2v-5a2 2 0 0 1 2-2h16a2 2 0 0 1 2 2v5a2 2 0 0 1-2 2h-2', 'M6 14h12v8H6z'],
  clipboard: ['M9 3h6v4H9z', 'M9 5H6a1 1 0 0 0-1 1v14a1 1 0 0 0 1 1h12a1 1 0 0 0 1-1V6a1 1 0 0 0-1-1h-3', 'm9 14 2 2 4-4'],
  sliders: ['M5 21v-7', 'M5 10V3', 'M12 21v-9', 'M12 8V3', 'M19 21v-5', 'M19 12V3', 'M2 14h6', 'M9 8h6', 'M16 16h6'],
  lock: ['M5 11h14v10H5z', 'M8 11V7a4 4 0 0 1 8 0v4', 'M12 15v2'],
  retry: ['M20 12a8 8 0 1 1-2.34-5.66', 'M20 4v5h-5'],
  // Copied from the Today HTML visual reference (TemplateHTML/Today).
  todayCart: [circle(9, 21, 1), circle(20, 21, 1), 'M1 2h3l2.5 13h14l2-10H5'],
  chevronLeft: ['m15 18-6-6 6-6'],
  chevronRight: ['m9 18 6-6-6-6'],
  close: ['M5 5l14 14M19 5 5 19'],
  info: [circle(12, 12, 9), 'M12 11v6M12 7h.01'],
  register: [rect(3, 9, 18, 12, 2), 'M6 9V3h12v6M7 13h10M7 17h5'],
  receipt: ['M5 3h14v18l-3-2-4 2-4-2-3 2zM9 8h6M9 12h6'],
  shoppingBag: ['M5 8h14l2 13H3zM9 9V6a3 3 0 0 1 6 0v3'],
  coins: [ellipse(8, 5, 5, 2), 'M3 5v8c0 1 2 2 5 2s5-1 5-2V5M3 9c0 1 2 2 5 2s5-1 5-2', ellipse(17, 15, 5, 2), 'M12 15v5c0 1 2 2 5 2s5-1 5-2v-5M12 18c0 1 2 2 5 2s5-1 5-2'],
  money: [rect(2, 5, 20, 14, 2), circle(12, 12, 3), 'M5 8h2M17 16h2'],
  bankBuilding: ['m2 9 10-6 10 6zM4 10v9M9 10v9M15 10v9M20 10v9M2 21h20'],
  alert: ['m12 3 10 18H2zM12 9v5M12 18h.01'],
  checkCircle: [circle(12, 12, 9), 'm8 12 3 3 5-6'],
  list: ['M8 6h13M8 12h13M8 18h13M3 6h.01M3 12h.01M3 18h.01'],
  users: [circle(9, 8, 4), 'M2 21v-2a7 7 0 0 1 14 0v2M17 4a4 4 0 0 1 0 8M18 14a6 6 0 0 1 4 6v1'],
  barChart: ['M3 21V10h4v11M10 21V3h4v18M17 21v-8h4v8M2 21h20'],
} satisfies Record<string, string[]>

export type IconName = keyof typeof icons
