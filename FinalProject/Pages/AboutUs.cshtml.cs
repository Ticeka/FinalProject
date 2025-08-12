using Microsoft.AspNetCore.Mvc.RazorPages;

namespace FinalProject.Pages
{
    public class AboutModel : PageModel
    {
        // ����ŢʶԵ� � ����Ҩ�ԧ�ҡ DB �������ѧ
        public int StatBreweries { get; private set; } = 128;
        public int StatDrinks { get; private set; } = 764;
        public int StatCities { get; private set; } = 47;
        public int StatReviews { get; private set; } = 5230;

        public void OnGet()
        {
            // TODO: �֧��Ҩ�ԧ�ҡ AppDbContext ��������� props
            // ������ҧ:
            // using var db = ... (��Ҩ� DI ��ҹ ctor ����)
            // StatDrinks = db.LocalBeers.Count();
        }
    }
}
