import { h, type FunctionalComponent } from 'vue'
import { icons, type IconName } from './icons'

/** Decorative stroke icon; the surrounding control carries the accessible name. */
const LineIcon: FunctionalComponent<{ name: IconName }> = props => h('svg', {
  class: 'line-icon',
  viewBox: '0 0 24 24',
  fill: 'none',
  stroke: 'currentColor',
  'stroke-width': 2,
  'stroke-linecap': 'round',
  'stroke-linejoin': 'round',
  'aria-hidden': 'true',
  focusable: 'false',
}, icons[props.name].map(d => h('path', { d })))

LineIcon.props = ['name']

export default LineIcon
