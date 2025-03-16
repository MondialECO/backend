using WebApp.Models.DatabaseModels;

namespace WebApp.Services.Interface
{
    public interface IBlogRepository
    {
        Task<BlogModel> GetByIdAsync(string id);
        Task<List<BlogModel>> GetAllAsync();
        Task AddAsync(BlogModel blogPost);
        Task UpdateAsync(BlogModel blogPost);
        Task DeleteAsync(string id);
        Task<List<BlogModel>> GetRecentPostsAsync(int count);
        Task<BlogModel> GetLatestPostAsync();
        Task<List<BlogModel>> GetPostsByCategoryAsync(string category);
        Task<List<BlogModel>> SearchPostsAsync(string searchTerm);
        Task<List<BlogModel>> GetPostsByPageAsync(int pageNumber, int pageSize);
        Task<List<string>> GetCategoriesAsync();


        Task<BlogCommentModel> GetByCommentIdAsync(string id);
        Task<List<BlogCommentModel>> GetAllCommentAsync();
        Task AddCommentAsync(BlogCommentModel comment);
        Task UpdateCommentAsync(BlogCommentModel comment);
        Task DeleteCommentAsync(string id);
        Task<List<BlogCommentModel>> GetCommentsByBlogPostIdAsync(string blogPostId);
        Task DeleteByBlogPostCommentIdAsync(string blogPostId); // Add this method
        Task<List<BlogCommentModel>> GetCommentsByParentCommentIdAsync(string parentCommentId); // Updated to return List<Comment>
    }
}
