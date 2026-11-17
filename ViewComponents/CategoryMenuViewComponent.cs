using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QL_ShopGiay.Data;

namespace QL_ShopGiay.ViewComponents
{
    public class CategoryMenuViewComponent : ViewComponent
    {
        private readonly ShopGiayDbContext _context;

        public CategoryMenuViewComponent(ShopGiayDbContext context)
        {
            _context = context;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            var danhMucCha = await _context.DanhMucs
                .Include(d => d.DanhMucCons)
                .Where(d => d.MaDanhMucCha == null)
                .OrderBy(d => d.MaDanhMuc)
                .ToListAsync();

            return View(danhMucCha);
        }
    }
}
