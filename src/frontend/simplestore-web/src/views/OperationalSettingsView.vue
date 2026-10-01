<script setup lang="ts">
import { computed, onBeforeUnmount, onMounted, reactive, ref, watch } from 'vue'
import { onBeforeRouteLeave, RouterLink, useRoute } from 'vue-router'
import { apiRequest } from '../api/client'
import type { StoreInfo, StoreOperationalSettings } from '../api/types'
import {
  densities, loginPhoto, modes, presets, previewCart, previewChips, previewNavigation, previewProducts, previewReceiptLines,
  referenceAppearance, referenceReceiptContact, settingsDemoEnabled, toggles, type AppearanceState,
} from '../settings/demo'

/**
 * Cài đặt: Giao diện follows TemplateHTML/Setting; Vận hành keeps the Store's real operational settings (D-102).
 * Store appearance has no persistence contract yet, so outside the preview its choices only drive the preview.
 */
const demo = settingsDemoEnabled
const route = useRoute()
const section = computed<'appearance' | 'operations'>(() => !demo.value && route.query.section === 'operations' ? 'operations' : 'appearance')

/* Giao diện */
const storeName = ref('')
const appearance = reactive<AppearanceState>({ ...referenceAppearance })
const logoAlternate = ref(false)
const photoAlternate = ref(false)
const prefersDark = ref(false)
const toastText = ref('')
let toastTimer: ReturnType<typeof setTimeout> | undefined
let darkQuery: MediaQueryList | null = null

const defaults = computed<AppearanceState>(() => demo.value ? referenceAppearance : { ...referenceAppearance, storeName: storeName.value || 'SimpleStore' })
const shownName = computed(() => appearance.storeName || 'SimpleStore')
const logoGlyph = computed(() => logoAlternate.value ? 'MA' : '▣')
const receiptContact = computed(() => demo.value ? referenceReceiptContact : [])
const dark = computed(() => appearance.mode === 'dark' || (appearance.mode === 'system' && prefersDark.value))
const themeStyle = computed(() => {
  const preset = presets.find(item => item.id === appearance.preset)!
  const density = densities.find(item => item.id === appearance.density)!
  return { '--accent': preset.accent[0], '--accent-strong': preset.accent[1], '--accent-soft': preset.accent[2], '--control-h': `${density.controlHeight}px` }
})
const unsupportedSave = 'Lưu giao diện chưa được hệ thống hỗ trợ.'

/* Vận hành */
const persisted = reactive({ allowNegativeStock: false, timeZoneId: '' })
const form = reactive({ allowNegativeStock: false, timeZoneId: '' })
const loaded = ref(false)
const loading = ref(!demo.value)
const loadError = ref('')
const saving = ref(false)
const saveError = ref('')
const timezoneChanged = computed(() => form.timeZoneId.trim() !== persisted.timeZoneId)
const dirty = computed(() => form.allowNegativeStock !== persisted.allowNegativeStock || timezoneChanged.value)
const offset = computed(() => {
  try {
    return new Intl.DateTimeFormat('en-US', { timeZone: form.timeZoneId.trim(), timeZoneName: 'shortOffset' })
      .formatToParts(new Date()).find(part => part.type === 'timeZoneName')?.value ?? ''
  } catch {
    return ''
  }
})

function toast(text: string) {
  toastText.value = text
  if (toastTimer) clearTimeout(toastTimer)
  toastTimer = setTimeout(() => { toastText.value = '' }, 1500)
}
function resetAppearance() {
  Object.assign(appearance, defaults.value)
  toast('Đã khôi phục mặc định')
}
function saveAppearance() {
  if (demo.value) toast('Đã lưu thay đổi trong dữ liệu mẫu')
}
function toggleLogo() {
  logoAlternate.value = !logoAlternate.value
  toast('Đã đổi logo minh họa')
}
function togglePhoto() {
  photoAlternate.value = !photoAlternate.value
  toast('Đã đổi ảnh nền minh họa trong dữ liệu mẫu')
}

async function load() {
  loading.value = true
  loadError.value = ''
  try {
    const [settings, store] = await Promise.all([
      apiRequest<StoreOperationalSettings>('/api/store/operational-settings'),
      apiRequest<StoreInfo>('/api/store/current'),
    ])
    Object.assign(persisted, { allowNegativeStock: settings.allowNegativeStock, timeZoneId: store.timeZoneId })
    Object.assign(form, persisted)
    storeName.value = store.name
    if (!demo.value) appearance.storeName = store.name
    loaded.value = true
  } catch (reason) {
    loadError.value = reason instanceof Error ? reason.message : 'Không thể tải thiết lập.'
  } finally {
    loading.value = false
  }
}

/** Sends only what changed; a failure keeps every unsaved value for retry. */
async function save() {
  if (!dirty.value || saving.value) return
  saving.value = true
  saveError.value = ''
  const timeZoneId = form.timeZoneId.trim()
  const tasks: Array<Promise<void>> = []
  if (form.allowNegativeStock !== persisted.allowNegativeStock) {
    tasks.push(apiRequest<StoreOperationalSettings>('/api/store/operational-settings/negative-stock', {
      method: 'PUT', body: JSON.stringify({ allowNegativeStock: form.allowNegativeStock }),
    }).then(result => { persisted.allowNegativeStock = result.allowNegativeStock }))
  }
  if (timeZoneId !== persisted.timeZoneId) {
    tasks.push(apiRequest<{ timeZoneId: string }>('/api/store/timezone', {
      method: 'PUT', body: JSON.stringify({ timeZoneId }),
    }).then(result => { persisted.timeZoneId = result.timeZoneId; form.timeZoneId = result.timeZoneId }))
  }
  const failure = (await Promise.allSettled(tasks)).find((result): result is PromiseRejectedResult => result.status === 'rejected')
  saving.value = false
  if (failure) {
    const reason = failure.reason instanceof Error ? failure.reason.message : 'Không thể lưu.'
    saveError.value = `${reason} Thông tin bạn nhập vẫn được giữ để thử lại.`
  } else {
    toast('Đã lưu thiết lập vận hành.')
  }
}
function cancel() {
  Object.assign(form, persisted)
  saveError.value = ''
}

function onDarkChange(event: MediaQueryListEvent) { prefersDark.value = event.matches }
function onBeforeUnload(event: BeforeUnloadEvent) {
  if (!dirty.value) return
  event.preventDefault()
  event.returnValue = ''
}

onBeforeRouteLeave(() => !dirty.value || window.confirm('Mục Vận hành có thay đổi chưa lưu. Rời trang và bỏ các thay đổi?'))
watch(demo, enabled => {
  logoAlternate.value = false
  photoAlternate.value = false
  Object.assign(appearance, defaults.value)
  if (!enabled && !loaded.value && !loading.value) void load()
})
onMounted(() => {
  if (typeof window.matchMedia === 'function') {
    darkQuery = window.matchMedia('(prefers-color-scheme: dark)')
    prefersDark.value = darkQuery.matches
    darkQuery.addEventListener('change', onDarkChange)
  }
  window.addEventListener('beforeunload', onBeforeUnload)
  if (!demo.value) void load()
})
onBeforeUnmount(() => {
  darkQuery?.removeEventListener('change', onDarkChange)
  window.removeEventListener('beforeunload', onBeforeUnload)
  if (toastTimer) clearTimeout(toastTimer)
})
</script>

<template>
  <section class="settings-page" :class="{ dark: section === 'appearance' && dark, square: section === 'appearance' && !appearance.rounded, tabular: section === 'appearance' && appearance.tabular }" :style="section === 'appearance' ? themeStyle : undefined">
    <nav v-if="!demo" class="settings-tabs" aria-label="Mục cài đặt">
      <RouterLink class="settings-tab" :class="{ active: section === 'appearance' }" :aria-current="section === 'appearance' ? 'page' : undefined" :to="{ path: '/settings/operations' }">Giao diện</RouterLink>
      <RouterLink class="settings-tab" :class="{ active: section === 'operations' }" :aria-current="section === 'operations' ? 'page' : undefined" :to="{ path: '/settings/operations', query: { section: 'operations' } }">Vận hành<span v-if="dirty" class="settings-dirty-dot" title="Có thay đổi chưa lưu" /></RouterLink>
    </nav>

    <template v-if="section === 'appearance'">
      <div class="settings-head"><h1>Giao diện</h1><p>Tùy chỉnh nhận diện cửa hàng mà vẫn giữ giao diện ổn định, dễ hỗ trợ và dễ sử dụng.</p></div>
      <div class="settings-layout">
        <section class="settings-card">
          <h2>Thương hiệu cửa hàng</h2><p class="settings-desc">Logo và tên cửa hàng sẽ hiển thị trên toàn bộ ứng dụng, bao gồm cả hóa đơn.</p>
          <div class="settings-brand-box">
            <button v-if="demo" class="settings-logo-box" type="button" @click="toggleLogo"><span><span class="settings-logo-big">▣</span><span>Thay đổi logo</span></span></button>
            <div v-else class="settings-logo-box readonly"><span><span class="settings-logo-big">▣</span><span>Logo mặc định</span></span></div>
            <div class="settings-field">
              <label for="settings-store-name">Tên cửa hàng</label>
              <input id="settings-store-name" v-model="appearance.storeName" :readonly="!demo" />
              <div class="settings-help">{{ demo ? 'Nên dùng logo vuông, định dạng PNG hoặc JPG, kích thước tối thiểu 512×512.' : 'Tên lấy từ thông tin cửa hàng. Đổi tên và logo chưa được hệ thống hỗ trợ.' }}</div>
            </div>
          </div>

          <div class="settings-section"><h3>Màu chủ đạo</h3><p>Chọn màu sắc thể hiện phong cách của cửa hàng của bạn.</p>
            <div class="settings-swatches">
              <div v-for="preset in presets" :key="preset.id" class="settings-swatch-wrap">
                <button class="settings-swatch" :class="{ active: appearance.preset === preset.id }" :style="{ background: preset.swatch }" type="button" :aria-label="preset.label" :aria-pressed="appearance.preset === preset.id" @click="appearance.preset = preset.id" />
                <div>{{ preset.label }}</div>
              </div>
            </div>
          </div>

          <div class="settings-section"><h3>Chế độ hiển thị</h3><p>Chọn giao diện sáng, tối hoặc theo cài đặt hệ thống.</p>
            <div class="settings-segmented" role="radiogroup" aria-label="Chế độ hiển thị">
              <button v-for="mode in modes" :key="mode.id" class="settings-seg" :class="{ active: appearance.mode === mode.id }" type="button" role="radio" :aria-checked="appearance.mode === mode.id" @click="appearance.mode = mode.id">{{ mode.icon }} {{ mode.label }}</button>
            </div>
          </div>

          <div class="settings-section"><h3>Mật độ giao diện</h3><p>Điều chỉnh khoảng cách hiển thị, cho phù hợp với thói quen sử dụng.</p>
            <div class="settings-density-grid" role="radiogroup" aria-label="Mật độ giao diện">
              <button v-for="density in densities" :key="density.id" class="settings-choice" :class="{ active: appearance.density === density.id }" type="button" role="radio" :aria-checked="appearance.density === density.id" @click="appearance.density = density.id">
                <span class="settings-choice-icon">{{ density.icon }}</span><span><b>{{ density.label }}</b><span>{{ density.text }}</span></span>
              </button>
            </div>
          </div>

          <div class="settings-section"><h3>Tùy chọn thêm</h3><p>Một số tùy chọn hiển thị nâng cao cho cửa hàng của bạn.</p>
            <div class="settings-extra-card"><div class="settings-extra-grid">
              <template v-for="(option, index) in toggles" :key="option.id">
                <button class="settings-toggle-row" type="button" role="switch" :aria-checked="appearance[option.id]" @click="appearance[option.id] = !appearance[option.id]">
                  <span class="settings-toggle" :class="{ on: appearance[option.id] }" /><span class="settings-toggle-copy"><b>{{ option.label }}</b><span>{{ option.text }}</span></span>
                </button>
                <div v-if="index === 0" class="settings-photo">
                  <img class="settings-photo-thumb" :src="loginPhoto" alt="Ảnh nền đăng nhập" />
                  <div>
                    <b>Ảnh nền đăng nhập</b>
                    <div class="settings-help settings-photo-help">{{ demo ? 'Tạo cảm giác gần gũi với thương hiệu.' : 'Ảnh minh họa; đổi ảnh nền chưa được hệ thống hỗ trợ.' }}</div>
                    <button v-if="demo" class="settings-btn small" type="button" @click="togglePhoto">↥ Thay đổi ảnh</button>
                  </div>
                </div>
              </template>
            </div></div>
          </div>

          <p v-if="!demo" class="settings-info-note">ℹ {{ unsupportedSave }} Các lựa chọn trên chỉ để xem trước, chưa áp dụng cho cửa hàng.</p>
          <div class="settings-actions">
            <button class="settings-btn" type="button" @click="resetAppearance">↶ Khôi phục mặc định</button>
            <button class="settings-btn primary" type="button" :disabled="!demo" :title="demo ? undefined : unsupportedSave" @click="saveAppearance">✓ Lưu thay đổi</button>
          </div>
        </section>

        <section class="settings-card settings-preview-card" aria-label="Xem trước giao diện">
          <div class="settings-preview-title"><div><h2>Xem trước</h2><p>Đây là giao diện minh họa với các tùy chỉnh hiện tại. Một số chi tiết có thể khác biệt nhỏ trong thực tế.</p></div></div>
          <div class="settings-preview-canvas">
            <div class="settings-badge">Màn bán hàng</div>
            <div class="settings-sales-mini">
              <aside class="settings-mini-side"><div class="settings-mini-brand">▣ SimpleStore</div><div v-for="(item, index) in previewNavigation" :key="item" class="settings-mini-nav" :class="{ active: index === 1 }">{{ item }}</div></aside>
              <div class="settings-mini-center">
                <div class="settings-mini-search">⌕ &nbsp; Tìm sản phẩm theo tên, mã vạch... <span>▥</span></div>
                <div class="settings-mini-chips"><span v-for="(chip, index) in previewChips" :key="chip" class="settings-mini-chip" :class="{ active: index === 0 }">{{ chip }}</span></div>
                <div class="settings-mini-products">
                  <div v-for="product in previewProducts" :key="product.name" class="settings-mini-product"><img :src="product.image" alt="" /><div>{{ product.name }}</div><b>{{ product.price }}</b></div>
                </div>
              </div>
              <aside class="settings-mini-cart">
                <h3>Hóa đơn</h3>
                <div v-for="row in previewCart" :key="row.name" class="settings-cart-row"><img :src="row.image" alt="" /><span>{{ row.name }}<br><small>{{ row.unit }}</small></span><b>{{ row.total }}</b></div>
                <div class="settings-cart-total">Tạm tính (4 món) <b>62.000đ</b></div>
                <div class="settings-checkout">▣ &nbsp; Thanh toán (F9)</div>
              </aside>
            </div>

            <div class="settings-preview-bottom">
              <div class="settings-preview-box"><div class="settings-badge">Đăng nhập</div>
                <div class="settings-login-preview">
                  <div class="settings-login-photo" :class="{ alternate: photoAlternate }" :style="{ backgroundImage: `url(${loginPhoto})` }" />
                  <div class="settings-login-card">
                    <div class="settings-login-logo">{{ logoGlyph }}</div>
                    <div class="settings-login-name">{{ shownName }}</div>
                    <div class="settings-login-sub">Quản lý bán hàng đơn giản, hiệu quả</div>
                    <div class="settings-login-input">Tên đăng nhập</div>
                    <div class="settings-login-input">Mật khẩu</div>
                    <div class="settings-login-btn">Đăng nhập</div>
                    <div class="settings-login-forgot">Quên mật khẩu?</div>
                  </div>
                </div>
              </div>
              <div class="settings-preview-box"><div class="settings-badge">Hóa đơn</div>
                <div class="settings-receipt">
                  <div class="settings-receipt-logo" :style="{ visibility: appearance.receiptLogo ? 'visible' : 'hidden' }">{{ logoGlyph }}</div>
                  <h3>{{ shownName }}</h3>
                  <p v-for="line in receiptContact" :key="line">{{ line }}</p>
                  <div class="settings-receipt-line">HÓA ĐƠN BÁN HÀNG<br>Số HĐ: HD000123 &nbsp;&nbsp; 14/05/2024 15:32</div>
                  <div v-for="line in previewReceiptLines" :key="line.label" class="settings-receipt-row"><span>{{ line.label }}</span><b>{{ line.value }}</b></div>
                  <div class="settings-receipt-line"><div class="settings-receipt-row"><span>Tổng tiền hàng</span><b>62.000đ</b></div><div class="settings-receipt-row"><span>Giảm giá</span><b>0đ</b></div></div>
                  <div class="settings-receipt-total"><span>TỔNG CỘNG:</span><span>62.000đ</span></div>
                  <p class="settings-receipt-thanks"><b>Cảm ơn quý khách!</b></p>
                </div>
              </div>
            </div>
          </div>
        </section>
      </div>
    </template>

    <template v-else>
      <div class="settings-head"><h1>Vận hành</h1><p>Thiết lập ảnh hưởng tới bán hàng và cách nhóm số liệu theo ngày của cửa hàng.</p></div>
      <section class="settings-card settings-ops-card">
        <p v-if="loading" class="settings-help" role="status">Đang tải thiết lập…</p>
        <div v-else-if="loadError" class="settings-error" role="alert">{{ loadError }} <button class="settings-btn small" type="button" @click="load">Thử lại</button></div>
        <template v-else>
          <div class="settings-ops-section">
            <h2>Bán hàng khi thiếu tồn kho</h2>
            <button class="settings-toggle-row" type="button" role="switch" :aria-checked="form.allowNegativeStock" :disabled="saving" @click="form.allowNegativeStock = !form.allowNegativeStock">
              <span class="settings-toggle" :class="{ on: form.allowNegativeStock }" /><span class="settings-toggle-copy"><b>Cho phép bán khi tồn kho không đủ</b><span>Khi tắt, đơn bán có sản phẩm thiếu tồn kho sẽ bị từ chối.</span></span>
            </button>
            <p v-if="form.allowNegativeStock" class="settings-info-note">ℹ Khi bật, một số sản phẩm có thể có tồn kho âm và độ tin cậy giá vốn có thể bị ảnh hưởng.</p>
          </div>
          <div class="settings-ops-section">
            <h2>Múi giờ cửa hàng</h2>
            <div class="settings-field">
              <label for="settings-timezone">Múi giờ (IANA)</label>
              <input id="settings-timezone" v-model="form.timeZoneId" list="settings-timezones" placeholder="Asia/Ho_Chi_Minh" autocomplete="off" :disabled="saving" />
              <datalist id="settings-timezones"><option value="Asia/Ho_Chi_Minh" /><option value="Asia/Bangkok" /><option value="Asia/Singapore" /><option value="America/New_York" /><option value="Europe/London" /></datalist>
              <div class="settings-help">{{ offset ? `Hiện tại: ${offset}. ` : '' }}Múi giờ quyết định ngày kinh doanh của các báo cáo, kể cả báo cáo cuối ngày.</div>
            </div>
            <p v-if="timezoneChanged" class="settings-warn-note">⚠ Đổi múi giờ sẽ thay đổi cách giao dịch được nhóm theo ngày kinh doanh, kể cả trong báo cáo cuối ngày.</p>
          </div>
          <div v-if="saveError" class="settings-error" role="alert">{{ saveError }}</div>
          <div class="settings-actions">
            <button class="settings-btn" type="button" :disabled="!dirty || saving" @click="cancel">Hủy thay đổi</button>
            <button class="settings-btn primary" type="button" :disabled="!dirty || saving" @click="save">{{ saving ? 'Đang lưu…' : '✓ Lưu thay đổi' }}</button>
          </div>
        </template>
      </section>
    </template>

    <div v-if="toastText" class="settings-toast" role="status">{{ toastText }}</div>
  </section>
</template>

<style scoped>
/* Values follow TemplateHTML/Setting/index.html (v1.0); theme variables stay inside this page. */
.settings-page{--accent:#0b9a68;--accent-strong:#087c56;--accent-soft:#e7f7f0;--page:#f6f9fb;--surface:#fff;--surface-2:#fbfdfe;--border:#dfe7ee;--text:#172033;--muted:#68768b;--control-h:36px;min-height:calc(100vh - var(--app-topbar-height,58px));background:var(--page);padding:12px 18px 20px;color:var(--text);font-size:16px}
.settings-page.dark{--page:#121820;--surface:#19212b;--surface-2:#202a35;--border:#34404e;--text:#eef3f7;--muted:#a5b2c1}
.settings-page.tabular{font-variant-numeric:tabular-nums}
.settings-page button{cursor:pointer}
.settings-page button:disabled{cursor:not-allowed}

.settings-tabs{display:flex;gap:4px;margin:0 0 12px;border-bottom:1px solid var(--border)}
.settings-tab{display:flex;align-items:center;gap:6px;margin-bottom:-1px;border-bottom:2px solid transparent;padding:8px 14px;color:var(--muted);font-size:14px;font-weight:700;text-decoration:none}
.settings-tab:hover{color:var(--text)}
.settings-tab.active{border-bottom-color:var(--accent);color:var(--accent-strong)}
.settings-dirty-dot{width:7px;height:7px;border-radius:50%;background:#d97706}

.settings-head h1{margin:0 0 6px;font-size:34px;font-weight:700;line-height:1;letter-spacing:-.04em}
.settings-head p{margin:0;color:var(--muted);font-size:14px}
.settings-layout{display:grid;grid-template-columns:minmax(0,.78fr) minmax(0,1fr);gap:16px;margin-top:14px}
.settings-card{border:1px solid var(--border);border-radius:14px;background:var(--surface);padding:18px;box-shadow:0 2px 7px #23384e08}
.settings-card h2{margin:0;font-size:21px;font-weight:700;letter-spacing:-.02em}
.settings-desc{margin:3px 0 12px;color:var(--muted);font-size:13px}
.settings-brand-box{display:grid;grid-template-columns:160px 1fr;gap:18px;border:1px solid var(--border);border-radius:10px;background:var(--surface-2);padding:14px}
.settings-logo-box{display:grid;height:104px;place-items:center;border:1px dashed #8ed4bb;border-radius:8px;background:transparent;color:var(--accent-strong);font-size:12px;font-weight:750;text-align:center}
.settings-logo-box>span>span{display:block}
.settings-logo-box.readonly{color:var(--muted)}
.settings-logo-big{font-size:34px}
.settings-field label{display:block;margin-bottom:7px;font-size:12px;font-weight:750}
.settings-field input{width:100%;height:var(--control-h);border:1px solid var(--border);border-radius:8px;background:var(--surface);padding:0 12px;color:var(--text)}
.settings-field input[readonly]{background:var(--surface-2)}
.settings-help{margin-top:8px;color:var(--muted);font-size:11px;line-height:1.5}
.settings-section{margin-top:16px}
.settings-section h3{margin:0 0 4px;font-size:18px;font-weight:700}
.settings-section>p{margin:0 0 10px;color:var(--muted);font-size:12px}
.settings-swatches{display:grid;grid-template-columns:repeat(6,1fr);gap:12px}
.settings-swatch-wrap{font-size:11px;font-weight:700;text-align:center}
/* The reference's swatch buttons have no width, so they render as 16px bars. */
.settings-swatch{position:relative;display:block;width:16px;height:48px;margin:0 auto;border:2px solid transparent;border-radius:9px;box-shadow:inset 0 0 0 1px #0000000a}
.settings-swatch.active{border-color:var(--accent);outline:2px solid var(--surface);box-shadow:0 0 0 3px var(--accent)}
.settings-swatch.active::after{position:absolute;inset:0;display:grid;place-items:center;color:#fff;font-size:18px;font-weight:900;content:"✓"}
.settings-segmented{display:grid;grid-template-columns:repeat(3,1fr);overflow:hidden;border:1px solid var(--border);border-radius:9px}
.settings-seg{height:44px;border:0;border-right:1px solid var(--border);background:var(--surface);color:var(--text);font-weight:700}
.settings-seg:last-child{border-right:0}
.settings-seg.active{background:var(--accent-soft);color:var(--accent-strong);box-shadow:inset 0 0 0 1px var(--accent)}
.settings-density-grid{display:grid;grid-template-columns:repeat(3,1fr);gap:10px}
.settings-choice{display:flex;min-height:72px;align-items:flex-start;gap:10px;border:1px solid var(--border);border-radius:9px;background:var(--surface-2);padding:11px;color:var(--text);text-align:center}
.settings-choice.active{border-color:var(--accent);background:var(--accent-soft)}
.settings-choice-icon{display:grid;width:34px;height:34px;flex:none;place-items:center;border-radius:8px;background:#edf2f7;color:#4b5f76;font-size:20px}
.settings-choice b{display:block;font-size:12px}
.settings-choice span span{display:block;margin-top:3px;color:var(--muted);font-size:10px;line-height:1.35}
.settings-extra-card{border:1px solid var(--border);border-radius:9px;background:var(--surface-2);padding:12px}
.settings-extra-grid{display:grid;grid-template-columns:1fr 1fr;gap:10px 22px}
.settings-toggle-row{display:flex;align-items:flex-start;gap:10px;border:0;background:transparent;padding:0;color:var(--text);text-align:left}
.settings-toggle-row:disabled{opacity:.6}
.settings-toggle{position:relative;width:44px;height:24px;flex:0 0 auto;border-radius:999px;background:#cfd8df;transition:.2s}
.settings-toggle::after{position:absolute;top:3px;left:3px;width:18px;height:18px;border-radius:50%;background:#fff;box-shadow:0 1px 3px #0003;transition:.2s;content:""}
.settings-toggle.on{background:var(--accent)}
.settings-toggle.on::after{left:23px}
.settings-toggle-copy b{display:block;font-size:12px}
.settings-toggle-copy span{display:block;margin-top:2px;color:var(--muted);font-size:10px;line-height:1.35}
.settings-photo{display:grid;grid-template-columns:64px 1fr;align-items:center;gap:10px}
.settings-photo-thumb{width:64px;height:50px;border-radius:7px;object-fit:cover}
.settings-photo b{font-size:12px}
.settings-photo-help{margin:2px 0 5px}
.settings-actions{display:flex;justify-content:space-between;gap:10px;margin-top:12px}
.settings-btn{height:40px;border:1px solid var(--border);border-radius:8px;background:var(--surface);padding:0 18px;color:var(--text);font-weight:750}
.settings-btn:hover:not(:disabled){filter:brightness(.98)}
.settings-btn.primary{min-width:190px;border-color:var(--accent);background:var(--accent);color:#fff}
.settings-btn:disabled{opacity:.55}
.settings-btn.small{height:30px;padding:0 18px;font-size:10px}
.settings-info-note{margin:12px 0 0;border:1px solid #d8eaf9;border-radius:8px;background:#eef7ff;padding:9px 11px;color:#35617f;font-size:12px;line-height:1.5}
.settings-warn-note{margin:10px 0 0;border:1px solid #f2ddaa;border-radius:8px;background:#fff5df;padding:9px 11px;color:#8c620b;font-size:12px;line-height:1.5}
.settings-error{margin:12px 0 0;border-radius:8px;background:#fff0f1;padding:9px 11px;color:#a94448;font-size:13px}

/* Preview */
.settings-preview-card{padding:14px}
.settings-preview-title{display:flex;align-items:flex-end;justify-content:space-between}
.settings-preview-title p{margin:2px 0 0;color:var(--muted);font-size:12px}
.settings-preview-canvas{margin-top:12px;border:1px solid var(--border);border-radius:11px;background:#f7fafc;padding:12px}
.settings-badge{display:inline-flex;margin-bottom:8px;border-radius:8px;background:var(--accent-soft);padding:6px 10px;color:var(--accent-strong);font-size:11px;font-weight:800}
.settings-sales-mini{display:grid;height:382px;grid-template-columns:108px 1fr 220px;overflow:hidden;border:1px solid var(--border);border-radius:10px;background:#fff}
.settings-mini-side{border-right:1px solid var(--border);background:#fbfcfd;padding:10px 8px}
.settings-mini-brand{margin:4px 0 14px;color:var(--accent-strong);font-size:10px;font-weight:850}
.settings-mini-nav{margin:2px 0;border-radius:5px;padding:7px 5px;color:#5e6c80;font-size:8px}
.settings-mini-nav.active{background:var(--accent-soft);color:var(--accent-strong);font-weight:800}
.settings-mini-center{min-width:0;padding:10px}
.settings-mini-search{display:flex;height:31px;align-items:center;border:1px solid #c8d5df;border-radius:7px;padding:0 9px;color:#8a96a7;font-size:8px}
.settings-mini-search span{margin-left:auto}
.settings-mini-chips{display:flex;gap:5px;margin:8px 0}
.settings-mini-chip{border:1px solid var(--border);border-radius:999px;padding:5px 8px;font-size:7px}
.settings-mini-chip.active{border-color:var(--accent);background:var(--accent);color:#fff}
.settings-mini-products{display:grid;grid-template-columns:repeat(3,1fr);gap:6px}
.settings-mini-product{min-height:91px;border:1px solid var(--border);border-radius:7px;background:#fff;padding:5px}
.settings-mini-product img{width:100%;height:54px;border-radius:5px;object-fit:cover}
.settings-mini-product div{margin-top:3px;font-size:7px}
.settings-mini-product b{display:block;margin-top:2px;color:var(--accent-strong);font-size:8px}
.settings-mini-cart{border-left:1px solid var(--border);background:#fff;padding:10px}
.settings-mini-cart h3{margin:0 0 8px;font-size:11px;font-weight:700}
.settings-cart-row{display:grid;grid-template-columns:24px 1fr auto;align-items:center;gap:6px;border-bottom:1px solid #edf1f4;padding:6px 0;font-size:7px}
.settings-cart-row img{width:24px;height:24px;border-radius:4px;object-fit:cover}
.settings-cart-total{margin-top:10px;font-size:8px}
.settings-checkout{display:grid;height:38px;margin-top:10px;place-items:center;border-radius:7px;background:var(--accent);color:#fff;font-size:16px;font-weight:800}
.settings-preview-bottom{display:grid;grid-template-columns:1.45fr .8fr;gap:10px;margin-top:10px}
.settings-preview-box{border:1px solid var(--border);border-radius:10px;background:#fff;padding:10px}
.settings-login-preview{display:grid;height:250px;grid-template-columns:1fr 1fr;overflow:hidden;border:1px solid var(--border);border-radius:8px}
.settings-login-photo{background-position:center;background-size:cover}
.settings-login-photo.alternate{filter:saturate(.75) contrast(1.08)}
.settings-login-card{display:flex;flex-direction:column;justify-content:center;background:#fff;padding:18px}
.settings-login-logo{color:var(--accent);font-size:28px;font-weight:900;text-align:center}
.settings-login-name{margin-top:4px;font-size:13px;font-weight:850;text-align:center}
.settings-login-sub{margin-bottom:12px;color:var(--muted);font-size:8px;text-align:center}
.settings-login-input{display:flex;height:31px;align-items:center;margin:4px 0;border:1px solid var(--border);border-radius:6px;padding:0 8px;color:#8a96a7;font-size:8px}
.settings-login-btn{display:grid;height:32px;margin-top:6px;place-items:center;border-radius:6px;background:var(--accent);color:#fff;font-size:9px;font-weight:800}
.settings-login-forgot{margin-top:6px;color:var(--accent);font-size:7px;text-align:center}
.settings-receipt{position:relative;min-height:250px;border-radius:6px;background:#fff;padding:14px 18px;color:#1e293b;font-family:"Courier New",monospace;box-shadow:0 4px 15px #1e374d17}
.settings-receipt::before{position:absolute;top:-6px;right:0;left:0;height:7px;background:linear-gradient(135deg,transparent 5px,#fff 0) 0 0/10px 10px repeat-x;content:""}
.settings-receipt-logo{color:var(--accent);font-family:Inter,sans-serif;font-size:20px;font-weight:900;text-align:center}
.settings-receipt h3{margin:4px 0;font-size:11px;font-weight:700;text-align:center}
.settings-receipt p{margin:1px;font-size:7px;text-align:center}
.settings-receipt-line{margin:8px 0;border-top:1px dashed #aaa;padding-top:6px;font-size:7px}
.settings-receipt-row{display:flex;justify-content:space-between;margin:3px 0;font-size:7px}
.settings-receipt-total{display:flex;justify-content:space-between;margin-top:7px;font-size:11px;font-weight:900}
.settings-receipt .settings-receipt-thanks{margin-top:10px;font-style:italic}

/* Dark mode, as in the reference */
.settings-page.dark .settings-preview-canvas{background:#10161d}
.settings-page.dark .settings-sales-mini,.settings-page.dark .settings-login-card,.settings-page.dark .settings-receipt{border-color:#364351;background:#1b242e;color:#eef3f7}
.settings-page.dark .settings-mini-side{background:#18212a}
.settings-page.dark .settings-mini-product,.settings-page.dark .settings-mini-cart,.settings-page.dark .settings-mini-search,.settings-page.dark .settings-choice,.settings-page.dark .settings-extra-card,.settings-page.dark .settings-brand-box,.settings-page.dark .settings-preview-box{border-color:#3b4654;background:#202a35}
/* The active density stays highlighted in dark mode; dark accent text keeps it readable on the soft fill. */
.settings-page.dark .settings-choice.active{border-color:var(--accent);background:var(--accent-soft);color:var(--accent-strong)}
.settings-page.dark .settings-choice.active span span{color:var(--accent-strong)}
/* "Bo góc mềm" off: square corners on the page's surfaces and the previews. */
.settings-page.square :is(.settings-card,.settings-brand-box,.settings-logo-box,.settings-swatch,.settings-segmented,.settings-choice,.settings-extra-card,.settings-btn,.settings-preview-canvas,.settings-badge,.settings-sales-mini,.settings-mini-product,.settings-preview-box,.settings-login-preview,.settings-receipt,.settings-checkout,.settings-login-btn,.settings-login-input,.settings-field input){border-radius:4px}

/* Vận hành */
.settings-ops-card{max-width:720px;margin-top:14px}
.settings-ops-section+.settings-ops-section{margin-top:18px;border-top:1px solid var(--border);padding-top:18px}
.settings-ops-section h2{margin-bottom:12px;font-size:18px}
.settings-ops-section .settings-toggle-copy b{font-size:14px}
.settings-ops-section .settings-toggle-copy span{font-size:12px}
.settings-ops-card .settings-field input{height:42px;font-size:14px}
.settings-ops-card .settings-actions{justify-content:flex-end;margin-top:18px}
.settings-ops-card .settings-btn.primary{min-width:160px}

.settings-toast{position:fixed;z-index:99;bottom:24px;left:50%;border-radius:8px;background:#132133;padding:10px 16px;color:#fff;font-size:11px;transform:translateX(-50%);box-shadow:0 10px 25px #0003}

/* The reference overflows between 1200px and ~1430px; switch to one column before the columns get too narrow. */
@media(max-width:1400px){.settings-layout{grid-template-columns:1fr}}
@media(max-width:760px){
  .settings-page{padding:12px}
  .settings-head h1{font-size:28px}
  .settings-swatches{grid-template-columns:repeat(3,1fr)}
  .settings-density-grid,.settings-extra-grid,.settings-brand-box,.settings-preview-bottom{grid-template-columns:1fr}
  .settings-sales-mini{height:auto;grid-template-columns:72px 1fr}
  .settings-mini-cart{grid-column:1/-1;border-top:1px solid var(--border);border-left:0}
  .settings-actions{flex-wrap:wrap}
  .settings-btn.primary{min-width:0;flex:1}
}
</style>
