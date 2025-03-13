using WebApp.DbContext;
using WebApp.InterfaceRepository;
using WebApp.Models.DatabaseModels;
using MongoDB.Driver;

namespace WebApp.Repository
{
    public class TestimonialRepository : ITestimonialRepository
    {
        private readonly MongoDbContext _context;

        public TestimonialRepository(MongoDbContext context)
        {
            _context = context;
        }

        public async Task AddOneAsync(TestimonialModel model)
        {
            if (model == null)
                throw new ArgumentNullException(nameof(model));

            await _context.Testimonials.InsertOneAsync(model);
        }

        public async Task DeleteAsync(string id)
        {
            await _context.Testimonials.DeleteOneAsync(f => f.Id == id);
        }

        public async Task<IEnumerable<TestimonialModel>> GetAllAsync()
        {
            return await _context.Testimonials.Find(_ => true).ToListAsync();
        }

        public async Task<IEnumerable<TestimonialModel>> GetBestAsync()
        {
            return await _context.Testimonials.Find(t => t.Point > 3).ToListAsync();
        }

        public async Task<TestimonialModel> GetByIdAsync(string id)
        {
            if (string.IsNullOrEmpty(id))
                throw new ArgumentException("Invalid ID", nameof(id));

            return await _context.Testimonials.Find(f => f.Id == id).FirstOrDefaultAsync();
        }

        public async Task UpdateAsync(string id, TestimonialModel model)
        {
            await _context.Testimonials.ReplaceOneAsync(f => f.Id == id, model);
        }
    }
}
