/**
 * Mdar Daily Operations — منطق صفحة العمليات اليومية
 *
 * التدفق الرئيسي:
 *   1. التحقق من JWT Token
 *   2. عرض Skeleton Loader
 *   3. استدعاء GET /api/dailyoperations/focus
 *   4. تحديث Header بفترة الصلاة الحالية + العداد
 *   5. رسم بطاقات المهام
 *   6. تحديث تلقائي كل 5 دقائق
 *   7. FAB → Modal → POST /api/dailyoperations/quick-add
 */

// ═══════════════════════════════════════════════════════════════════
//  State
// ═══════════════════════════════════════════════════════════════════

const App = (() => {

    // الحالة الداخلية
    let state = {
        tasks:          [],
        currentPeriod:  null,
        prayerSchedule: null,
        hasPrayerSchedule: false,
        contextFilter:  'Anywhere',
        periodFilter:   'current',
        loading:        false,
        refreshTimer:   null
    };

    // ── بيانات فترات الصلاة ──────────────────────────────────────────────
    const PERIOD_META = {
        AfterFajr:    { label: 'بعد الفجر',   emoji: '🌙', next: 'Duha'         },
        Duha:         { label: 'وقت الضحى',   emoji: '☀️', next: 'AfterDhuhr'   },
        AfterDhuhr:   { label: 'بعد الظهر',   emoji: '🌤', next: 'AfterAsr'     },
        AfterAsr:     { label: 'بعد العصر',   emoji: '🌿', next: 'AfterMaghrib' },
        AfterMaghrib: { label: 'بعد المغرب',  emoji: '🌅', next: 'AfterIsha'    },
        AfterIsha:    { label: 'بعد العشاء',  emoji: '🌃', next: 'AfterFajr'    }
    };

    const PRIORITY_META = {
        Critical: { label: 'حرجة',    color: 'Critical' },
        High:     { label: 'عالية',   color: 'High'     },
        Medium:   { label: 'متوسطة', color: 'Medium'   },
        Low:      { label: 'منخفضة', color: 'Low'      }
    };

    const CONTEXT_META = {
        Anywhere: { label: 'الكل',   icon: '🌐' },
        Home:     { label: 'المنزل', icon: '🏠' },
        Office:   { label: 'المكتب', icon: '💼' },
        Outside:  { label: 'خارج',  icon: '🚶' },
        Online:   { label: 'أونلاين',icon: '💻' }
    };

    // ═══════════════════════════════════════════════════════════════════
    //  Init
    // ═══════════════════════════════════════════════════════════════════

    function init() {
        // تحقق من التوكن
        if (!ApiClient.hasToken()) {
            showAuthOverlay();
            return;
        }

        // عرّض الصفحة
        document.getElementById('app').classList.remove('hidden');

        // ساعة حية
        updateClock();
        setInterval(updateClock, 1000);

        // تحميل البيانات
        loadFocusTasks();

        // تحديث تلقائي كل 5 دقائق
        state.refreshTimer = setInterval(loadFocusTasks, 5 * 60 * 1000);

        // أحداث
        bindEvents();
    }

    // ═══════════════════════════════════════════════════════════════════
    //  Data Loading
    // ═══════════════════════════════════════════════════════════════════

    async function loadFocusTasks() {
        if (state.loading) return;
        state.loading = true;
        showSkeleton();

        try {
            const data = await ApiClient.getFocusTasks({
                prayerTimeContext:      state.periodFilter,
                contextTag:            state.contextFilter === 'Anywhere' ? undefined : state.contextFilter,
                maxResults:            20,
                includeWeightBreakdown: true
            });

            state.tasks          = data.tasks            || [];
            state.currentPeriod  = data.currentPeriod;
            state.prayerSchedule = data.prayerSchedule;
            state.hasPrayerSchedule = data.hasPrayerSchedule;

            renderAll(data);

        } catch (err) {
            showError(err.message || 'فشل تحميل المهام');
            renderErrorState();
        } finally {
            state.loading = false;
        }
    }

    // ═══════════════════════════════════════════════════════════════════
    //  Render: All
    // ═══════════════════════════════════════════════════════════════════

    function renderAll(data) {
        renderPeriodHeader(data.currentPeriod, data.currentPeriodName);
        renderPrayerTimeline(data.prayerSchedule);
        renderTasks(data.tasks);
        updateStatsBar(data);
    }

    // ─── Header ─────────────────────────────────────────────────────────────

    function renderPeriodHeader(period, periodName) {
        const header = document.getElementById('period-header');
        // تغيير theme class
        header.className = header.className.replace(/period-\w+/g, '');
        header.classList.add(`period-${period}`);

        const meta = PERIOD_META[period] || {};
        const el   = document.getElementById('period-display');
        if (el) {
            el.innerHTML = `
                <span class="period-emoji">${meta.emoji || '⏰'}</span>
                <div>
                    <div class="period-name">${periodName || meta.label || period}</div>
                    <div class="period-subtext" id="next-prayer-countdown">...</div>
                </div>
            `;
        }
    }

    // ─── Prayer Timeline ─────────────────────────────────────────────────────

    function renderPrayerTimeline(schedule) {
        const container = document.getElementById('prayer-timeline');
        if (!container || !schedule) {
            if (container) container.innerHTML = '<p class="text-white/50 text-xs text-center py-2">لم يُسجَّل جدول صلاة لليوم</p>';
            return;
        }

        const prayers = [
            { key: 'fajrTime',    label: 'فجر'   },
            { key: 'sunriseTime', label: 'شروق'  },
            { key: 'dhuhrTime',   label: 'ظهر'   },
            { key: 'asrTime',     label: 'عصر'   },
            { key: 'maghribTime', label: 'مغرب'  },
            { key: 'ishaTime',    label: 'عشاء'  }
        ];

        container.innerHTML = `
            <div class="flex justify-between items-end mb-1 px-1">
                ${prayers.map(p => `
                    <div class="text-center">
                        <div class="text-white/60 text-xs mb-1">${formatTime(schedule[p.key])}</div>
                        <div class="text-white/80 text-xs font-bold">${p.label}</div>
                    </div>
                `).join('')}
            </div>
            <div class="timeline-track relative">
                <div class="timeline-fill" id="timeline-fill" style="width:${calcTimelineProgress(schedule)}%"></div>
            </div>
        `;
    }

    function calcTimelineProgress(schedule) {
        if (!schedule) return 0;
        const now   = new Date();
        const fajr  = parseTime(schedule.fajrTime);
        const isha  = parseTime(schedule.ishaTime);

        const dayStart = fajr.getTime();
        const dayEnd   = isha.getTime() + 90 * 60000; // +90 دقيقة بعد العشاء

        const progress = (now.getTime() - dayStart) / (dayEnd - dayStart);
        return Math.max(0, Math.min(100, progress * 100));
    }

    // ─── Stats Bar ───────────────────────────────────────────────────────────

    function updateStatsBar(data) {
        const total = data.tasks?.length || 0;
        const emergency = data.excludedEmergencyCount || 0;

        const el = document.getElementById('stats-bar');
        if (el) {
            el.innerHTML = `
                <span class="text-xs text-slate-500">
                    <span class="font-bold text-slate-700">${total}</span> مهمة
                    ${emergency > 0 ? `· <span class="text-red-500 font-bold">${emergency}</span> طوارئ` : ''}
                    ${!data.hasPrayerSchedule ? '· <span class="text-amber-500">⚠ لا يوجد جدول صلاة</span>' : ''}
                </span>
                <button onclick="App.refresh()" class="text-xs text-slate-400 hover:text-slate-600 flex items-center gap-1">
                    <svg xmlns="http://www.w3.org/2000/svg" class="w-3 h-3" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><polyline points="23 4 23 10 17 10"/><path d="M20.49 15a9 9 0 1 1-2.12-9.36L23 10"/></svg>
                    تحديث
                </button>
            `;
        }
    }

    // ─── Task Cards ──────────────────────────────────────────────────────────

    function renderTasks(tasks) {
        const container = document.getElementById('tasks-container');
        if (!container) return;

        if (!tasks || tasks.length === 0) {
            container.innerHTML = renderEmptyState();
            return;
        }

        container.innerHTML = `
            <div class="tasks-grid" id="tasks-grid">
                ${tasks.map(task => renderTaskCard(task)).join('')}
            </div>
        `;
    }

    function renderTaskCard(task) {
        const priority    = task.priority   || 'Medium';
        const pMeta       = PRIORITY_META[priority] || PRIORITY_META.Medium;
        const contextMeta = CONTEXT_META[task.contextTag] || CONTEXT_META.Anywhere;
        const rankClass   = task.rank <= 3 ? `rank-${task.rank}` : 'rank-other';
        const dueDateText = formatDueDate(task.dueDate);
        const periodText  = task.preferredPrayerPeriod
            ? (PERIOD_META[task.preferredPrayerPeriod]?.emoji || '') + ' ' + (PERIOD_META[task.preferredPrayerPeriod]?.label || '')
            : '';

        return `
        <div class="task-card priority-${priority}" data-id="${task.id}" id="card-${task.id}">

            <!-- Header: Rank + Priority + Context -->
            <div class="flex items-center justify-between mb-3">
                <div class="flex items-center gap-2">
                    <span class="rank-badge ${rankClass}">${task.rank}</span>
                    <span class="badge-${priority} text-xs font-bold px-2 py-0.5 rounded-full">
                        ${pMeta.label}
                    </span>
                    ${task.isEmergency ? '<span class="text-xs bg-red-100 text-red-600 font-bold px-2 py-0.5 rounded-full">🚨 طوارئ</span>' : ''}
                </div>
                <div class="flex items-center gap-1.5 text-slate-400">
                    <span title="${task.contextTag}" class="text-sm">${contextMeta.icon}</span>
                    ${task.isPomodoroCompatible ? '<span title="قابلة للطماطم" class="text-sm">🍅</span>' : ''}
                </div>
            </div>

            <!-- Title -->
            <h3 class="font-bold text-slate-800 text-[0.95rem] leading-snug mb-2 line-clamp-2">
                ${escapeHtml(task.title)}
            </h3>

            <!-- Meta Row -->
            <div class="flex flex-wrap items-center gap-2 mb-3">
                ${dueDateText ? `<span class="text-xs ${dueDateText.overdue ? 'text-red-500 font-bold' : 'text-slate-400'}">📅 ${dueDateText.text}</span>` : ''}
                ${task.isPomodoroCompatible ? `
                    <span class="text-xs text-slate-400">
                        🍅 ${task.completedPomodoros ?? 0}/${task.estimatedPomodoros ?? '?'}
                    </span>` : ''}
                ${periodText ? `<span class="text-xs text-slate-400">${periodText}</span>` : ''}
                <span class="weight-chip mr-auto">⚡ ${Math.round(task.priorityWeight)}</span>
            </div>

            <!-- Weight Explanation -->
            ${task.weightBreakdown?.explanation ? `
                <p class="text-xs text-slate-400 bg-slate-50 rounded-xl px-3 py-2 mb-3 leading-relaxed">
                    ${escapeHtml(task.weightBreakdown.explanation)}
                </p>` : ''}

            <!-- Action Buttons -->
            <div class="flex gap-2 mt-auto">
                ${task.isPomodoroCompatible ? `
                    <button class="btn-start" onclick="App.startPomodoro('${task.id}', '${escapeAttr(task.title)}')">
                        ▶ ابدأ جلسة
                    </button>` : ''}
                <button class="btn-done" onclick="App.completeTask('${task.id}')">
                    ✓ إنجاز
                </button>
            </div>
        </div>`;
    }

    function renderEmptyState() {
        return `
        <div class="empty-state col-span-full">
            <span class="empty-icon">✅</span>
            <p class="text-lg font-bold text-slate-500">لا توجد مهام مجدولة</p>
            <p class="text-sm text-slate-400 mt-1">استخدم زر "إضافة مهمة سريعة" لإضافة أولى مهامك</p>
        </div>`;
    }

    function renderErrorState() {
        const container = document.getElementById('tasks-container');
        if (!container) return;
        container.innerHTML = `
        <div class="empty-state col-span-full">
            <span class="empty-icon">⚠️</span>
            <p class="text-lg font-bold text-slate-500">فشل تحميل المهام</p>
            <button onclick="App.refresh()" class="mt-3 px-4 py-2 bg-slate-800 text-white rounded-xl text-sm font-bold">
                إعادة المحاولة
            </button>
        </div>`;
    }

    // ═══════════════════════════════════════════════════════════════════
    //  Skeleton Loader
    // ═══════════════════════════════════════════════════════════════════

    function showSkeleton() {
        const container = document.getElementById('tasks-container');
        if (!container) return;
        const skeletonCard = `
            <div class="bg-white rounded-2xl p-4 border border-slate-100">
                <div class="flex items-center gap-2 mb-3">
                    <div class="skeleton w-6 h-6 rounded-full"></div>
                    <div class="skeleton h-5 w-20 rounded-full"></div>
                </div>
                <div class="skeleton h-4 w-full mb-2 rounded-lg"></div>
                <div class="skeleton h-4 w-2/3 mb-4 rounded-lg"></div>
                <div class="flex gap-2">
                    <div class="skeleton h-9 flex-1 rounded-xl"></div>
                    <div class="skeleton h-9 flex-1 rounded-xl"></div>
                </div>
            </div>`;
        container.innerHTML = `
            <div class="tasks-grid">
                ${Array(6).fill(skeletonCard).join('')}
            </div>`;
    }

    // ═══════════════════════════════════════════════════════════════════
    //  Clock + Countdown
    // ═══════════════════════════════════════════════════════════════════

    function updateClock() {
        const el = document.getElementById('live-clock');
        if (el) {
            const now = new Date();
            el.textContent = now.toLocaleTimeString('ar-SA', {
                hour: '2-digit', minute: '2-digit', second: '2-digit'
            });
        }
        updateCountdown();
    }

    function updateCountdown() {
        if (!state.prayerSchedule) return;
        const schedule = state.prayerSchedule;
        const el = document.getElementById('next-prayer-countdown');
        if (!el) return;

        const now    = new Date();
        const prayers = ['fajrTime','sunriseTime','dhuhrTime','asrTime','maghribTime','ishaTime'];
        const labels  = ['الفجر','الشروق','الظهر','العصر','المغرب','العشاء'];

        let nextLabel = '';
        let remainMs  = Infinity;

        for (let i = 0; i < prayers.length; i++) {
            const t = parseTime(schedule[prayers[i]]);
            const diff = t.getTime() - now.getTime();
            if (diff > 0 && diff < remainMs) {
                remainMs  = diff;
                nextLabel = labels[i];
            }
        }

        if (remainMs === Infinity || !nextLabel) {
            el.textContent = 'يتبقى حتى فجر الغد';
            return;
        }

        const h = Math.floor(remainMs / 3600000);
        const m = Math.floor((remainMs % 3600000) / 60000);
        const s = Math.floor((remainMs % 60000) / 1000);

        const parts = [];
        if (h > 0) parts.push(`${h} س`);
        if (m > 0) parts.push(`${m} د`);
        if (h === 0) parts.push(`${s} ث`);

        el.textContent = `⏱ ${parts.join(' ')} حتى ${nextLabel}`;
    }

    // ═══════════════════════════════════════════════════════════════════
    //  Modal: Quick Add
    // ═══════════════════════════════════════════════════════════════════

    function openModal() {
        document.getElementById('modal-backdrop').classList.add('visible');
        document.getElementById('modal-sheet').classList.add('visible');
        document.body.style.overflow = 'hidden';

        // Focus on title input
        setTimeout(() => {
            document.getElementById('new-task-title')?.focus();
        }, 350);
    }

    function closeModal() {
        document.getElementById('modal-backdrop').classList.remove('visible');
        document.getElementById('modal-sheet').classList.remove('visible');
        document.body.style.overflow = '';
        resetModalForm();
    }

    function resetModalForm() {
        document.getElementById('new-task-title').value = '';
        document.getElementById('new-task-due').value = '';
        document.getElementById('new-task-period').value = '';
        document.getElementById('new-task-pomodoro').checked = true;
        setSelectedPriority('Medium');
    }

    function setSelectedPriority(p) {
        document.querySelectorAll('.priority-btn').forEach(btn => {
            btn.classList.toggle('selected', btn.dataset.priority === p);
        });
    }

    async function submitQuickAdd() {
        const title = document.getElementById('new-task-title').value.trim();
        if (!title) {
            showToast('اكتب عنوان المهمة أولاً', 'error');
            document.getElementById('new-task-title').focus();
            return;
        }

        const selectedPriority = document.querySelector('.priority-btn.selected')?.dataset.priority || 'Medium';
        const pomodoro = document.getElementById('new-task-pomodoro').checked;
        const dueRaw   = document.getElementById('new-task-due').value;
        const period   = document.getElementById('new-task-period').value || null;

        const body = {
            title,
            priority:             selectedPriority,
            isPomodoroCompatible: pomodoro,
            dueDate:              dueRaw || null,
            preferredPrayerPeriod: period || null,
            contextTag:           'Anywhere'
        };

        const btn = document.getElementById('modal-submit-btn');
        btn.disabled = true;
        btn.innerHTML = '<span class="animate-spin inline-block">⏳</span> جارٍ الإضافة...';

        try {
            const result = await ApiClient.quickAddTask(body);
            closeModal();
            showToast(`✓ تمت الإضافة: ${title} (وزن: ${Math.round(result.initialPriorityWeight)})`, 'success');
            // أعِد تحميل القائمة
            setTimeout(loadFocusTasks, 300);
        } catch (err) {
            showToast(err.message || 'فشلت الإضافة', 'error');
        } finally {
            btn.disabled = false;
            btn.innerHTML = '+ إضافة المهمة';
        }
    }

    // ═══════════════════════════════════════════════════════════════════
    //  Task Actions
    // ═══════════════════════════════════════════════════════════════════

    async function completeTask(taskId) {
        const card = document.getElementById(`card-${taskId}`);
        try {
            await ApiClient.completeTask(taskId);

            // Animation: fade out card
            if (card) {
                card.classList.add('completing');
                setTimeout(() => {
                    card.remove();
                    // إذا فرغت القائمة
                    if (!document.querySelector('.task-card')) {
                        document.getElementById('tasks-container').innerHTML = renderEmptyState();
                    }
                }, 300);
            }
            showToast('✓ أحسنت! تم إنجاز المهمة', 'success');
        } catch (err) {
            showToast(err.message || 'فشل تحديث الحالة', 'error');
        }
    }

    async function startPomodoro(taskId, taskTitle) {
        showToast(`🍅 بدأت جلسة طماطم: ${taskTitle}`, 'info');
        // هنا يمكن لاحقاً استدعاء API لإنشاء PomodoroSession
        // ApiClient.post('/pomodorosessions', { taskItemId: taskId, plannedDurationMinutes: 25 })
    }

    // ═══════════════════════════════════════════════════════════════════
    //  Filters
    // ═══════════════════════════════════════════════════════════════════

    function setContextFilter(ctx) {
        state.contextFilter = ctx;
        document.querySelectorAll('.ctx-chip').forEach(chip =>
            chip.classList.toggle('active', chip.dataset.ctx === ctx)
        );
        loadFocusTasks();
    }

    function setPeriodFilter(period) {
        state.periodFilter = period;
        document.querySelectorAll('.period-tab').forEach(tab =>
            tab.classList.toggle('active', tab.dataset.period === period)
        );
        loadFocusTasks();
    }

    function refresh() {
        loadFocusTasks();
    }

    // ═══════════════════════════════════════════════════════════════════
    //  Auth Overlay
    // ═══════════════════════════════════════════════════════════════════

    function showAuthOverlay() {
        document.getElementById('auth-overlay').classList.remove('hidden');
        document.getElementById('app').classList.add('hidden');
    }

    function handleTokenSubmit() {
        const token = document.getElementById('token-input').value.trim();
        if (!token) {
            showToast('أدخل رمز JWT أولاً', 'error');
            return;
        }
        ApiClient.setToken(token);
        document.getElementById('auth-overlay').classList.add('hidden');
        document.getElementById('app').classList.remove('hidden');
        init();
    }

    // ═══════════════════════════════════════════════════════════════════
    //  Toast Notifications
    // ═══════════════════════════════════════════════════════════════════

    function showToast(message, type = 'info') {
        const container = document.getElementById('toast-container');
        const toast = document.createElement('div');
        toast.className = `toast toast-${type}`;
        toast.textContent = message;
        container.appendChild(toast);

        setTimeout(() => {
            toast.style.animation = 'toastOut .3s ease forwards';
            setTimeout(() => toast.remove(), 300);
        }, 3500);
    }

    // ═══════════════════════════════════════════════════════════════════
    //  Events
    // ═══════════════════════════════════════════════════════════════════

    function bindEvents() {
        // FAB
        document.getElementById('fab-btn')?.addEventListener('click', openModal);

        // Backdrop click → close modal
        document.getElementById('modal-backdrop')?.addEventListener('click', closeModal);

        // ESC → close modal
        document.addEventListener('keydown', (e) => {
            if (e.key === 'Escape') closeModal();
        });

        // Priority buttons in modal
        document.querySelectorAll('.priority-btn').forEach(btn => {
            btn.addEventListener('click', () => setSelectedPriority(btn.dataset.priority));
        });

        // Modal submit on Enter
        document.getElementById('new-task-title')?.addEventListener('keydown', (e) => {
            if (e.key === 'Enter') submitQuickAdd();
        });
    }

    // ═══════════════════════════════════════════════════════════════════
    //  Utilities
    // ═══════════════════════════════════════════════════════════════════

    function formatDueDate(dateStr) {
        if (!dateStr) return null;
        const due   = new Date(dateStr + 'T00:00:00');
        const today = new Date(); today.setHours(0,0,0,0);
        const diff  = Math.round((due - today) / 86400000);

        if (diff < 0) return { text: `متأخرة ${Math.abs(diff)} يوم`, overdue: true };
        if (diff === 0) return { text: 'اليوم', overdue: false };
        if (diff === 1) return { text: 'غداً',  overdue: false };
        if (diff <= 7)  return { text: `بعد ${diff} أيام`, overdue: false };
        return { text: due.toLocaleDateString('ar-SA', { month: 'short', day: 'numeric' }), overdue: false };
    }

    function formatTime(timeStr) {
        if (!timeStr) return '--:--';
        const [h, m] = timeStr.split(':');
        const d = new Date(); d.setHours(+h, +m, 0);
        return d.toLocaleTimeString('ar-SA', { hour: '2-digit', minute: '2-digit' });
    }

    function parseTime(timeStr) {
        const [h, m] = (timeStr || '00:00').split(':');
        const d = new Date(); d.setHours(+h, +m, 0, 0);
        return d;
    }

    function escapeHtml(str) {
        const d = document.createElement('div');
        d.appendChild(document.createTextNode(str || ''));
        return d.innerHTML;
    }

    function escapeAttr(str) {
        return (str || '').replace(/'/g, '&#39;').replace(/"/g, '&quot;');
    }

    // ── Public API ───────────────────────────────────────────────────────────
    return {
        init,
        refresh,
        completeTask,
        startPomodoro,
        setContextFilter,
        setPeriodFilter,
        openModal,
        closeModal,
        submitQuickAdd,
        handleTokenSubmit,
        showAuthOverlay,
        showToast
    };

})();

// ═══════════════════════════════════════════════════════════════════
//  Bootstrap
// ═══════════════════════════════════════════════════════════════════
document.addEventListener('DOMContentLoaded', App.init);
