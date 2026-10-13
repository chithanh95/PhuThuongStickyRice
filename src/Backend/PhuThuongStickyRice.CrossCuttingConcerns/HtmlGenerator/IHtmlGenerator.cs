using System.Threading.Tasks;

namespace PhuThuongStickyRice.CrossCuttingConcerns.HtmlGenerator
{
    public interface IHtmlGenerator
    {
        Task<string> GenerateAsync(string template, object model);
    }
}
