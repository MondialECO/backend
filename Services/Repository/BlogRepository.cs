using WebApp.Models.DatabaseModels;
using WebApp.Services.Interface;
using MongoDB.Driver;
using WebApp.DbContext;
using System.Xml.Linq;
namespace WebApp.Services.Repository
{
    public class BlogRepository : IBlogRepository
    {
        private readonly MongoDbContext _context;

        public BlogRepository(MongoDbContext context)
        {
            _context = context;
        }

        public async Task<BlogModel> GetByIdAsync(string id)
        {
            var filter = Builders<BlogModel>.Filter.Eq(x => x.Id, id);
            return await _context.Blogs.Find(filter).FirstOrDefaultAsync();
        }

        public async Task<List<BlogModel>> GetAllAsync()
        {
            return await _context.Blogs.Find(_ => true)
                                   .SortByDescending(bp => bp.CreatedDate)
                                   .ToListAsync();
        }

        public async Task<BlogModel> GetLatestPostAsync()
        {
            return await _context.Blogs.Find(_ => true)
                                   .SortByDescending(bp => bp.CreatedDate)
                                   .FirstOrDefaultAsync();
        }
        public async Task<List<string>> GetCategoriesAsync()
        {
            return await _context.Blogs.Distinct<string>("BlogCategory", Builders<BlogModel>.Filter.Empty).ToListAsync();
        }

        public async Task AddAsync(BlogModel blogPost)
        {
            await _context.Blogs.InsertOneAsync(blogPost);
        }

        public async Task UpdateAsync(BlogModel blogPost)
        {
            var filter = Builders<BlogModel>.Filter.Eq(x => x.Id, blogPost.Id);
            var update = Builders<BlogModel>.Update.Set(x => x.Title, blogPost.Title)
                                                  .Set(x => x.Content, blogPost.Content)
                                                  .Set(x => x.ImageUrl, blogPost.ImageUrl)
                                                  .Set(x => x.Category, blogPost.Category);
            await _context.Blogs.UpdateOneAsync(filter, update);
        }

        public async Task DeleteAsync(string id)
        {
            var filter = Builders<BlogModel>.Filter.Eq(x => x.Id, id);
            await _context.Blogs.DeleteOneAsync(filter);
        }

        // Filter Category 
        public async Task<List<BlogModel>> GetPostsByCategoryAsync(string category)
        {
            var filter = Builders<BlogModel>.Filter.Eq(x => x.Category, category);
            return await _context.Blogs.Find(filter).ToListAsync();
        }

        // search work needed
        public async Task<List<BlogModel>> SearchPostsAsync(string searchTerm)
        {
            var filter = Builders<BlogModel>.Filter.Or(
                Builders<BlogModel>.Filter.Regex(x => x.Title, new MongoDB.Bson.BsonRegularExpression(searchTerm, "i")),
                Builders<BlogModel>.Filter.Regex(x => x.Content, new MongoDB.Bson.BsonRegularExpression(searchTerm, "i"))
            );
            return await _context.Blogs.Find(filter).ToListAsync();
        }
        // search work needed
        public async Task<List<BlogModel>> GetPostsByPageAsync(int pageNumber, int pageSize)
        {
            return await _context.Blogs.Find(_ => true)
                                   .Skip((pageNumber - 1) * pageSize)
                                   .Limit(pageSize)
                                   .ToListAsync();
        }

        public async Task<List<BlogModel>> GetRecentPostsAsync(int count)
        {
            return await _context.Blogs.Find(_ => true)
                                   .SortByDescending(x => x.CreatedDate)
                                   .Limit(count)
                                   .ToListAsync();
        }

        public async Task<BlogCommentModel> GetByCommentIdAsync(string id)
        {
            var filter = Builders<BlogCommentModel>.Filter.Eq(x => x.Id, id);
            return await _context.BlogsComments.Find(filter).FirstOrDefaultAsync();
        }

        public async Task<List<BlogCommentModel>> GetAllCommentAsync()
        {
            return await _context.BlogsComments.Find(_ => true).ToListAsync();
        }

        public async Task AddCommentAsync(BlogCommentModel comment)
        {
            await _context.BlogsComments.InsertOneAsync(comment);
        }

        public async Task UpdateCommentAsync(BlogCommentModel comment)
        {
            var filter = Builders<BlogCommentModel>.Filter.Eq(x => x.Id, comment.Id);
            await _context.BlogsComments.ReplaceOneAsync(filter, comment);
        }

        public async Task DeleteCommentAsync(string id)
        {
            var filter = Builders<BlogCommentModel>.Filter.Eq(x => x.Id, id);
            await _context.BlogsComments.DeleteOneAsync(filter);
        }

        public async Task<List<BlogCommentModel>> GetCommentsByBlogPostIdAsync(string blogPostId)
        {
            var filter = Builders<BlogCommentModel>.Filter.Eq(x => x.BlogPostId, blogPostId);
            return await _context.BlogsComments.Find(filter).ToListAsync();
        }

        public async Task DeleteByBlogPostCommentIdAsync(string blogPostId)
        {
            var filter = Builders<BlogCommentModel>.Filter.Eq(c => c.BlogPostId, blogPostId);
            await _context.BlogsComments.DeleteManyAsync(filter);
        }

        public async Task<List<BlogCommentModel>> GetCommentsByParentCommentIdAsync(string parentCommentId)
        {
            var filter = Builders<BlogCommentModel>.Filter.Eq(c => c.ParentCommentId, parentCommentId);
            return await _context.BlogsComments.Find(filter).ToListAsync();
        }
    }
}
