/**
 * Mdar API Client — طبقة تجريد لجميع نداءات الـ API
 *
 * الميزات:
 *   ✓ JWT Token مُخزَّن في localStorage
 *   ✓ إعادة التوجيه التلقائي عند انتهاء الصلاحية (401)
 *   ✓ دعم GET / POST / PATCH / DELETE
 *   ✓ معالجة موحَّدة للأخطاء مع ProblemDetails
 *   ✓ IIFE — لا يلوّث الـ Global Scope
 */
const ApiClient = (() => {
    const BASE_URL  = '/api';
    const TOKEN_KEY = 'mdar_token';

    // ── Token Management ───────────────────────────────────────────────────

    const getToken  = () => localStorage.getItem(TOKEN_KEY);
    const setToken  = (token) => localStorage.setItem(TOKEN_KEY, token);
    const clearToken = () => localStorage.removeItem(TOKEN_KEY);
    const hasToken  = () => !!getToken();

    // ── Build Auth Headers ─────────────────────────────────────────────────

    const authHeaders = () => ({
        'Content-Type': 'application/json',
        'Authorization': `Bearer ${getToken()}`
    });

    // ── Response Handler ───────────────────────────────────────────────────

    const handleResponse = async (res) => {
        // 401 → رمز منتهي أو مفقود → أعِد التوجيه
        if (res.status === 401) {
            clearToken();
            window.App?.showAuthOverlay?.();
            throw new Error('انتهت صلاحية الجلسة. يرجى تسجيل الدخول مجدداً.');
        }

        // 204 No Content
        if (res.status === 204) return null;

        let data;
        try   { data = await res.json(); }
        catch { data = {}; }

        if (!res.ok) {
            // RFC 7807 ProblemDetails
            const msg = data.detail || data.title || data.message || `خطأ ${res.status}`;
            const err = new Error(msg);
            err.status = res.status;
            err.data   = data;
            throw err;
        }

        return data;
    };

    // ── Build URL with Query Params ────────────────────────────────────────

    const buildUrl = (endpoint, params = {}) => {
        const url = new URL(BASE_URL + endpoint, window.location.origin);
        Object.entries(params).forEach(([k, v]) => {
            if (v !== null && v !== undefined && v !== '') {
                url.searchParams.append(k, v);
            }
        });
        return url.toString();
    };

    // ── Public HTTP Methods ────────────────────────────────────────────────

    const get = (endpoint, params = {}) =>
        fetch(buildUrl(endpoint, params), { headers: authHeaders() })
            .then(handleResponse);

    const post = (endpoint, body = {}) =>
        fetch(buildUrl(endpoint), {
            method: 'POST',
            headers: authHeaders(),
            body: JSON.stringify(body)
        }).then(handleResponse);

    const patch = (endpoint, body = {}) =>
        fetch(buildUrl(endpoint), {
            method: 'PATCH',
            headers: authHeaders(),
            body: JSON.stringify(body)
        }).then(handleResponse);

    const del = (endpoint) =>
        fetch(buildUrl(endpoint), {
            method: 'DELETE',
            headers: authHeaders()
        }).then(handleResponse);

    // ── Domain-Specific Methods ────────────────────────────────────────────

    /** جلب جلسة التركيز اليومية */
    const getFocusTasks = (params = {}) =>
        get('/dailyoperations/focus', {
            prayerTimeContext:      params.prayerTimeContext      || 'current',
            contextTag:             params.contextTag             || 'Anywhere',
            maxResults:             params.maxResults             || 20,
            includeWeightBreakdown: params.includeWeightBreakdown ?? true
        });

    /** إضافة مهمة سريعة */
    const quickAddTask = (data) =>
        post('/dailyoperations/quick-add', data);

    /** إتمام مهمة */
    const completeTask = (taskId) =>
        patch(`/tasks/${taskId}/complete`);

    /** تبديل وضع الطوارئ */
    const toggleEmergency = (taskId) =>
        patch(`/tasks/${taskId}/emergency`);

    // ── Public API ─────────────────────────────────────────────────────────

    return {
        /* Auth */
        setToken, getToken, clearToken, hasToken,

        /* Generic */
        get, post, patch, delete: del,

        /* Domain */
        getFocusTasks,
        quickAddTask,
        completeTask,
        toggleEmergency
    };
})();
