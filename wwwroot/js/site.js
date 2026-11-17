// =========================================================================
// SNEAKER BUZZ / STRIDELAB - Frontend JavaScript (AJAX / Fetch API)
// =========================================================================

document.addEventListener('DOMContentLoaded', () => {
    initServerToast();
    initLiveSearch();
    initQuickAddToCart();
    initCopyVoucherButtons();
    initProductDetailVariants();
    initCartPageActions();
});

function showToast(message, type = 'success') {
    const toastEl = document.getElementById('liveToast');
    const toastMsg = document.getElementById('liveToastMessage');
    const toastIcon = document.getElementById('liveToastIcon');
    if (!toastEl || !toastMsg) return;

    toastMsg.textContent = message;
    if (toastIcon) {
        toastIcon.className = type === 'warning' || type === 'error'
            ? 'bi bi-exclamation-triangle-fill text-warning'
            : 'bi bi-check-square-fill text-white';
    }

    const toast = bootstrap.Toast.getOrCreateInstance(toastEl, { delay: 3200 });
    toast.show();
}

function formatVND(amount) {
    return Number(amount || 0).toLocaleString('vi-VN') + '₫';
}

function updateCartBadge(count, total) {
    const badge = document.getElementById('headerCartBadge');
    const totalEl = document.getElementById('headerCartTotal');
    if (badge && typeof count === 'number') {
        badge.textContent = count;
    }
    if (totalEl && typeof total === 'number') {
        totalEl.textContent = formatVND(total);
    }
}

function initServerToast() {
    const serverToast = document.querySelector('[data-server-toast]');
    if (serverToast) {
        const msg = serverToast.getAttribute('data-server-toast');
        const type = serverToast.getAttribute('data-server-toast-type') || 'success';
        if (msg) showToast(msg, type);
    }
}

// Thanh tìm kiếm gợi ý trực tiếp (AJAX Live Search)
function initLiveSearch() {
    const input = document.getElementById('liveSearchInput');
    const dropdown = document.getElementById('liveSearchResults');
    if (!input || !dropdown) return;

    let debounceTimer = null;

    input.addEventListener('input', () => {
        const q = input.value.trim();
        clearTimeout(debounceTimer);

        if (q.length < 1) {
            dropdown.classList.add('d-none');
            dropdown.innerHTML = '';
            return;
        }

        debounceTimer = setTimeout(async () => {
            try {
                const res = await fetch(`/Product/SearchSuggest?q=${encodeURIComponent(q)}`);
                if (!res.ok) return;
                const items = await res.json();

                if (!items || items.length === 0) {
                    dropdown.innerHTML = `
                        <div class="p-3 text-center text-muted small">
                            Không tìm thấy sản phẩm khớp với "<strong>${q}</strong>"
                        </div>`;
                    dropdown.classList.remove('d-none');
                    return;
                }

                let html = `<div class="px-3 py-2 bg-light-gray border-bottom small fw-bold text-uppercase" style="font-size:11px;">SẢN PHẨM GỢI Ý</div>`;
                items.forEach(item => {
                    html += `
                        <a href="/Product/Detail/${item.maSanPham}" class="live-search-item d-flex align-items-center gap-3 p-2 px-3 text-decoration-none border-bottom">
                            <img src="${item.hinhAnh}" alt="${item.tenSanPham}" class="border bg-light-gray" style="width: 46px; height: 46px; object-fit: cover;" />
                            <div class="flex-grow-1 overflow-hidden">
                                <div class="fw-bold text-dark small text-truncate">${item.tenSanPham}</div>
                                <div class="text-muted" style="font-size: 11px;">${item.thuongHieu} • ${item.tenDanhMuc}</div>
                            </div>
                            <div class="fw-bold text-sale small text-nowrap">${formatVND(item.giaBan)}</div>
                        </a>`;
                });
                html += `
                    <a href="/Product?tuKhoa=${encodeURIComponent(q)}" class="d-block text-center py-2 small fw-bold text-dark text-decoration-none bg-light-gray">
                        XEM TẤT CẢ KẾT QUẢ CHO "${q.toUpperCase()}" →
                    </a>`;

                dropdown.innerHTML = html;
                dropdown.classList.remove('d-none');
            } catch (err) {
                console.error(err);
            }
        }, 200);
    });

    document.addEventListener('click', (e) => {
        if (!input.contains(e.target) && !dropdown.contains(e.target)) {
            dropdown.classList.add('d-none');
        }
    });
}

// Thêm nhanh vào giỏ hàng từ Product Card
function initQuickAddToCart() {
    document.querySelectorAll('.btn-quick-add').forEach(btn => {
        btn.addEventListener('click', async (e) => {
            e.preventDefault();
            e.stopPropagation();
            const productId = parseInt(btn.getAttribute('data-product-id') || '0', 10);
            if (!productId) return;

            try {
                const res = await fetch('/Cart/AddToCart', {
                    method: 'POST',
                    headers: { 'Content-Type': 'application/json' },
                    body: JSON.stringify({ maSanPham: productId, maBienThe: 0, soLuong: 1 })
                });
                const data = await res.json();
                if (data.success) {
                    updateCartBadge(data.cartCount, data.cartTotal);
                    showToast(data.message, 'success');
                } else {
                    showToast(data.message || 'Không thể thêm vào giỏ hàng', 'warning');
                }
            } catch (err) {
                showToast('Lỗi kết nối máy chủ.', 'error');
            }
        });
    });
}

function initCopyVoucherButtons() {
    document.querySelectorAll('.btn-copy-voucher').forEach(btn => {
        btn.addEventListener('click', () => {
            const code = btn.getAttribute('data-code');
            if (!code) return;
            navigator.clipboard?.writeText(code);
            showToast(`Đã sao chép mã giảm giá: ${code}`, 'success');
        });
    });
}

// Chọn Biến thể (Màu sắc & Size giày) tại trang Chi Tiết Sản Phẩm
function initProductDetailVariants() {
    const variants = window.PRODUCT_VARIANTS;
    if (!variants || !Array.isArray(variants) || variants.length === 0) return;

    const colorBtns = document.querySelectorAll('.variant-color-btn');
    const sizeBtns = document.querySelectorAll('.variant-size-btn');
    const selectedColorNameEl = document.getElementById('selectedColorName');
    const selectedSizeNameEl = document.getElementById('selectedSizeName');
    const priceEl = document.getElementById('detailPrice');
    const skuEl = document.getElementById('detailSku');
    const stockBadgeEl = document.getElementById('detailStockBadge');
    const variantIdInput = document.getElementById('selectedVariantId');
    const qtyInput = document.getElementById('detailQuantity');
    const btnMinus = document.getElementById('btnQtyMinus');
    const btnPlus = document.getElementById('btnQtyPlus');
    const btnAddCart = document.getElementById('btnAddToCartDetail');
    const btnBuyNow = document.getElementById('btnBuyNowDetail');

    let currentColorId = parseInt(document.querySelector('.variant-color-btn.active')?.getAttribute('data-color-id') || variants[0].MaMau, 10);
    let currentSizeId = parseInt(document.querySelector('.variant-size-btn.active')?.getAttribute('data-size-id') || variants[0].MaKichCo, 10);

    function refreshVariantState() {
        sizeBtns.forEach(sBtn => {
            const sId = parseInt(sBtn.getAttribute('data-size-id'), 10);
            const match = variants.find(v => v.MaMau === currentColorId && v.MaKichCo === sId);
            if (!match || match.SoLuongTon <= 0) {
                sBtn.classList.add('disabled-variant');
            } else {
                sBtn.classList.remove('disabled-variant');
            }
        });

        let activeVariant = variants.find(v => v.MaMau === currentColorId && v.MaKichCo === currentSizeId);
        if (!activeVariant) {
            activeVariant = variants.find(v => v.MaMau === currentColorId && v.SoLuongTon > 0)
                         || variants.find(v => v.MaMau === currentColorId);
            if (activeVariant) {
                currentSizeId = activeVariant.MaKichCo;
            }
        }

        colorBtns.forEach(cBtn => {
            cBtn.classList.toggle('active', parseInt(cBtn.getAttribute('data-color-id'), 10) === currentColorId);
        });
        sizeBtns.forEach(sBtn => {
            sBtn.classList.toggle('active', parseInt(sBtn.getAttribute('data-size-id'), 10) === currentSizeId);
        });

        if (activeVariant) {
            if (selectedColorNameEl) selectedColorNameEl.textContent = activeVariant.TenMau;
            if (selectedSizeNameEl) selectedSizeNameEl.textContent = activeVariant.SoKichCo;
            if (priceEl) priceEl.textContent = formatVND(activeVariant.GiaBan);
            if (skuEl) skuEl.textContent = activeVariant.MaSKU;
            if (variantIdInput) variantIdInput.value = activeVariant.MaBienThe;

            if (activeVariant.SoLuongTon > 0) {
                if (stockBadgeEl) {
                    stockBadgeEl.innerHTML = `<span class="text-dark fw-semibold">Tình trạng: <strong class="text-success">Còn hàng (${activeVariant.SoLuongTon} đôi)</strong></span>`;
                }
                if (qtyInput) {
                    qtyInput.max = activeVariant.SoLuongTon;
                    if (parseInt(qtyInput.value, 10) > activeVariant.SoLuongTon) {
                        qtyInput.value = activeVariant.SoLuongTon;
                    }
                }
                if (btnAddCart) btnAddCart.disabled = false;
                if (btnBuyNow) btnBuyNow.disabled = false;
            } else {
                if (stockBadgeEl) {
                    stockBadgeEl.innerHTML = `<span class="text-danger fw-bold">Tạm hết hàng</span>`;
                }
                if (btnAddCart) btnAddCart.disabled = true;
                if (btnBuyNow) btnBuyNow.disabled = true;
            }
        }
    }

    colorBtns.forEach(btn => {
        btn.addEventListener('click', () => {
            currentColorId = parseInt(btn.getAttribute('data-color-id'), 10);
            refreshVariantState();
        });
    });

    sizeBtns.forEach(btn => {
        btn.addEventListener('click', () => {
            currentSizeId = parseInt(btn.getAttribute('data-size-id'), 10);
            refreshVariantState();
        });
    });

    if (btnMinus && qtyInput) {
        btnMinus.addEventListener('click', () => {
            const val = Math.max(1, parseInt(qtyInput.value || '1', 10) - 1);
            qtyInput.value = val;
        });
    }

    if (btnPlus && qtyInput) {
        btnPlus.addEventListener('click', () => {
            const max = parseInt(qtyInput.max || '99', 10);
            const val = Math.min(max, parseInt(qtyInput.value || '1', 10) + 1);
            qtyInput.value = val;
        });
    }

    async function sendDetailAddToCart(redirectToCheckout = false) {
        const maBienThe = parseInt(variantIdInput?.value || '0', 10);
        const soLuong = Math.max(1, parseInt(qtyInput?.value || '1', 10));

        if (!maBienThe) {
            showToast('Vui lòng chọn Màu sắc và Kích cỡ giày!', 'warning');
            return;
        }

        try {
            const res = await fetch('/Cart/AddToCart', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ maBienThe, maSanPham: 0, soLuong })
            });
            const data = await res.json();
            if (data.success) {
                updateCartBadge(data.cartCount, data.cartTotal);
                if (redirectToCheckout) {
                    window.location.href = '/Cart/Checkout';
                } else {
                    showToast(data.message, 'success');
                }
            } else {
                showToast(data.message || 'Không thể thêm vào giỏ hàng', 'warning');
            }
        } catch (err) {
            showToast('Lỗi kết nối máy chủ.', 'error');
        }
    }

    if (btnAddCart) {
        btnAddCart.addEventListener('click', () => sendDetailAddToCart(false));
    }
    if (btnBuyNow) {
        btnBuyNow.addEventListener('click', () => sendDetailAddToCart(true));
    }

    refreshVariantState();
}

// Xử lý Giỏ hàng AJAX
function initCartPageActions() {
    const cartWrapper = document.getElementById('cartMainWrapper');
    if (!cartWrapper) return;

    function updateSummaryDOM(data) {
        const subEl = document.getElementById('cartSummarySubtotal');
        const discEl = document.getElementById('cartSummaryDiscount');
        const totalEl = document.getElementById('cartSummaryTotal');
        const countTextEl = document.getElementById('cartItemCountText');

        if (subEl && data.tamTinh !== undefined) subEl.textContent = formatVND(data.tamTinh);
        if (discEl && data.tienGiam !== undefined) discEl.textContent = '-' + formatVND(data.tienGiam);
        if (totalEl && data.tongCong !== undefined) totalEl.textContent = formatVND(data.tongCong);
        if (countTextEl && data.cartCount !== undefined) countTextEl.textContent = data.cartCount;
        if (data.cartCount !== undefined) updateCartBadge(data.cartCount, data.tamTinh);
    }

    async function changeItemQty(variantId, newQty) {
        try {
            const res = await fetch('/Cart/UpdateQuantity', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ maBienThe: variantId, soLuong: newQty })
            });
            const data = await res.json();

            if (data.cartCount === 0) {
                window.location.reload();
                return;
            }

            const inputEl = document.querySelector(`.cart-qty-input[data-variant-id="${variantId}"]`);
            const rowSubtotalEl = document.querySelector(`.cart-item-subtotal[data-variant-id="${variantId}"]`);
            if (inputEl && data.soLuong !== undefined) inputEl.value = data.soLuong;
            if (rowSubtotalEl && data.thanhTien !== undefined) rowSubtotalEl.textContent = formatVND(data.thanhTien);

            updateSummaryDOM(data);

            if (!data.success && data.message) {
                showToast(data.message, 'warning');
            }
        } catch (err) {
            showToast('Không thể cập nhật số lượng.', 'error');
        }
    }

    document.querySelectorAll('.btn-cart-qty-minus').forEach(btn => {
        btn.addEventListener('click', () => {
            const id = parseInt(btn.getAttribute('data-variant-id'), 10);
            const input = document.querySelector(`.cart-qty-input[data-variant-id="${id}"]`);
            const current = parseInt(input?.value || '1', 10);
            if (current > 1) {
                changeItemQty(id, current - 1);
            }
        });
    });

    document.querySelectorAll('.btn-cart-qty-plus').forEach(btn => {
        btn.addEventListener('click', () => {
            const id = parseInt(btn.getAttribute('data-variant-id'), 10);
            const input = document.querySelector(`.cart-qty-input[data-variant-id="${id}"]`);
            const current = parseInt(input?.value || '1', 10);
            changeItemQty(id, current + 1);
        });
    });

    document.querySelectorAll('.cart-qty-input').forEach(input => {
        input.addEventListener('change', () => {
            const id = parseInt(input.getAttribute('data-variant-id'), 10);
            const val = Math.max(1, parseInt(input.value || '1', 10));
            changeItemQty(id, val);
        });
    });

    document.querySelectorAll('.btn-cart-remove').forEach(btn => {
        btn.addEventListener('click', async () => {
            const id = parseInt(btn.getAttribute('data-variant-id'), 10);
            try {
                const res = await fetch('/Cart/RemoveItem', {
                    method: 'POST',
                    headers: { 'Content-Type': 'application/json' },
                    body: JSON.stringify({ maBienThe: id })
                });
                const data = await res.json();
                if (data.cartCount === 0) {
                    window.location.reload();
                    return;
                }
                const row = document.querySelector(`.cart-item-row[data-variant-id="${id}"]`);
                if (row) row.remove();
                updateSummaryDOM(data);
                showToast(data.message || 'Đã xóa sản phẩm khỏi giỏ hàng.');
            } catch (err) {
                showToast('Không thể xóa sản phẩm.', 'error');
            }
        });
    });

    const voucherInput = document.getElementById('voucherCodeInput');
    const btnApplyVoucher = document.getElementById('btnApplyVoucher');
    const btnRemoveVoucher = document.getElementById('btnRemoveVoucher');
    const appliedBadge = document.getElementById('appliedVoucherBadge');
    const appliedCodeText = document.getElementById('appliedCodeText');
    const appliedPercentText = document.getElementById('appliedPercentText');

    async function applyVoucherCode(code) {
        if (!code || !code.trim()) {
            showToast('Vui lòng nhập mã khuyến mãi!', 'warning');
            return;
        }
        try {
            const res = await fetch('/Cart/ApplyVoucher', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ maCode: code.trim() })
            });
            const data = await res.json();
            if (data.success) {
                if (voucherInput) voucherInput.value = data.maCode;
                if (appliedCodeText) appliedCodeText.textContent = data.maCode;
                if (appliedPercentText) appliedPercentText.textContent = data.phanTramGiam;
                if (appliedBadge) appliedBadge.classList.remove('d-none');
                updateSummaryDOM(data);
                showToast(data.message, 'success');
            } else {
                showToast(data.message, 'warning');
            }
        } catch (err) {
            showToast('Không thể kiểm tra mã khuyến mãi.', 'error');
        }
    }

    if (btnApplyVoucher && voucherInput) {
        btnApplyVoucher.addEventListener('click', () => applyVoucherCode(voucherInput.value));
    }

    document.querySelectorAll('.btn-quick-voucher').forEach(btn => {
        btn.addEventListener('click', () => {
            const code = btn.getAttribute('data-code');
            if (code) applyVoucherCode(code);
        });
    });

    if (btnRemoveVoucher) {
        btnRemoveVoucher.addEventListener('click', async () => {
            try {
                const res = await fetch('/Cart/RemoveVoucher', { method: 'POST' });
                const data = await res.json();
                if (data.success) {
                    if (voucherInput) voucherInput.value = '';
                    if (appliedBadge) appliedBadge.classList.add('d-none');
                    updateSummaryDOM(data);
                    showToast(data.message, 'success');
                }
            } catch (err) {
                console.error(err);
            }
        });
    }
}
