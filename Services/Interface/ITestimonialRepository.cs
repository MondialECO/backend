using WebApp.Models.DatabaseModels;

namespace WebApp.InterfaceRepository
{
    public interface ITestimonialRepository
    {
        Task<IEnumerable<TestimonialModel>> GetAllAsync();
        Task<TestimonialModel> GetByIdAsync(string id);
        Task AddOneAsync(TestimonialModel model);
        Task UpdateAsync(string id, TestimonialModel model);
        Task DeleteAsync(string id);
        Task<IEnumerable<TestimonialModel>> GetBestAsync();
    }
}
