using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using WebApp.Models;
using WebApp.Models.DatabaseModels;
using WebApp.Services.Interface;

namespace WebApp.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class BlogController : ControllerBase
    {
        private readonly IBlogRepository _blogRepository;
        private readonly UserManager<ApplicationUser> _userManager;

        public BlogController(IBlogRepository blogRepository, UserManager<ApplicationUser> userManager)
        {
            _blogRepository = blogRepository;
            _userManager = userManager;
        }


        // Get all blog posts
        [HttpGet("all")]
        public async Task<IActionResult> GetAll()
        {
            var blogs = await _blogRepository.GetAllAsync();
            return Ok(blogs);
        }

        // Get a single blog post by ID
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return NotFound(new { Message = "Invalid blog ID" });
            }

            var blogPost = await _blogRepository.GetByIdAsync(id);
            if (blogPost == null)
            {
                return NotFound(new { Message = "Blog post not found" });
            }

            List<BlogCommentModel> comments = await _blogRepository.GetCommentsByBlogPostIdAsync(id);

            var model = new
            {
                BlogPost = blogPost,
                Comments = comments
            };

            return Ok(model);
        }



        // Get recent blog posts
        [HttpGet("recent/{count}")]
        public async Task<IActionResult> GetRecent(int count)
        {
            var blogs = await _blogRepository.GetRecentPostsAsync(count);
            return Ok(blogs);
        }

        // Get blog posts by category
        [HttpGet("category/{category}")]
        public async Task<IActionResult> GetByCategory(string category)
        {
            var blogs = await _blogRepository.GetPostsByCategoryAsync(category);
            return Ok(blogs);
        }

        // Search blog posts
        [HttpGet("search")]
        public async Task<IActionResult> Search([FromQuery] string term)
        {
            var blogs = await _blogRepository.SearchPostsAsync(term);
            return Ok(blogs);
        }


        // Create a new blog post
        [HttpPost("create")]
        [Authorize] // Requires authentication
        public async Task<IActionResult> Create( BlogCreateModel model)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            string imageUrl = null;
            if (model.ImageFile != null)
            {
                var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images/blogs");
                Directory.CreateDirectory(uploadsFolder);

                string fileExtension = Path.GetExtension(model.ImageFile.FileName).ToLower();
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };

                if (!allowedExtensions.Contains(fileExtension))
                {
                    return BadRequest(new { Message = "Invalid file type. Allowed: JPG, PNG, GIF." });
                }

                if (model.ImageFile.Length > 3 * 1024 * 1024) // 3MB limit
                {
                    return BadRequest(new { Message = "File size exceeds 3MB limit." });
                }

                string fileName = $"{model.ImageFile.FileName}";
                string filePath = Path.Combine(uploadsFolder, fileName);

                try
                {
                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await model.ImageFile.CopyToAsync(stream);
                    }
                    imageUrl = $"/images/blogs/{fileName}";
                }
                catch (Exception ex)
                {
                    return StatusCode(500, new { Message = "File upload failed.", Error = ex.Message });
                }

            }
            var user = await _userManager.FindByIdAsync(model.CreatorId);
            if (user == null)
                return NotFound(new { Message = "User not found" });

            var blogPost = new BlogModel
            {
                Id = Guid.NewGuid().ToString(),
                Title = model.Title,
                Content = model.Content,
                Category = model.BlogCategory,
                ImageUrl = imageUrl,
                CreatorId = user.Id.ToString(),
                CreatedDate = DateTime.UtcNow
            };

            await _blogRepository.AddAsync(blogPost);
            return CreatedAtAction(nameof(GetById), new { Message = "Blog post created successfully", id = blogPost.Id }, blogPost);
        }

        [HttpPut("update/{id}")]
        [Authorize]
        public async Task<IActionResult> Update(string id, BlogCreateModel model)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var existingBlog = await _blogRepository.GetByIdAsync(id);
            if (existingBlog == null)
                return NotFound(new { Message = "Blog not found" });

            string imageUrl = existingBlog.ImageUrl;

            if (model.ImageFile != null)
            {
                var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images/blogs");
                Directory.CreateDirectory(uploadsFolder);

                string fileExtension = Path.GetExtension(model.ImageFile.FileName).ToLower();
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };

                if (!allowedExtensions.Contains(fileExtension))
                {
                    return BadRequest(new { Message = "Invalid file type. Allowed: JPG, PNG, GIF." });
                }

                if (model.ImageFile.Length > 3 * 1024 * 1024)
                {
                    return BadRequest(new { Message = "File size exceeds 3MB limit." });
                }

                if (!string.IsNullOrEmpty(existingBlog.ImageUrl))
                {
                    var oldImagePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", existingBlog.ImageUrl.TrimStart('/'));
                    if (System.IO.File.Exists(oldImagePath))
                    {
                        System.IO.File.Delete(oldImagePath);
                    }
                }

                // Save new image
                string fileName = $"{model.ImageFile.FileName}";
                string filePath = Path.Combine(uploadsFolder, fileName);

                try
                {
                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await model.ImageFile.CopyToAsync(stream);
                    }
                    imageUrl = $"/images/blogs/{fileName}";
                }
                catch (Exception ex)
                {
                    return StatusCode(500, new { Message = "File upload failed.", Error = ex.Message });
                }
            }

            // Update blog post details
            existingBlog.Title = model.Title;
            existingBlog.Content = model.Content;
            existingBlog.Category = model.BlogCategory;
            existingBlog.ImageUrl = imageUrl;
            existingBlog.UpdateDate = DateTime.UtcNow;

            await _blogRepository.UpdateAsync(existingBlog);
            return Ok(new { Message = "Blog post updated successfully", Blog = existingBlog });
        }


        [HttpDelete("delete/{id}")]
        [Authorize]
        public async Task<IActionResult> Delete(string id)
        {
            var blog = await _blogRepository.GetByIdAsync(id);
            if (blog == null)
            {
                return NotFound(new { Message = "Blog not found" });
            }

            if (!string.IsNullOrEmpty(blog.ImageUrl))
            {
                var imagePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", blog.ImageUrl.TrimStart('/'));
                if (System.IO.File.Exists(imagePath))
                {
                    System.IO.File.Delete(imagePath);
                }
            }

            await _blogRepository.DeleteByBlogPostCommentIdAsync(id);
            await _blogRepository.DeleteAsync(id);
            return Ok(new { Message = "Blog post deleted successfully" });
        }




        // Comments Area

        // Get all comments for a blog post
        [HttpGet("{id}/comments")]
        public async Task<IActionResult> GetComments(string id)
        {
            var comments = await _blogRepository.GetCommentsByBlogPostIdAsync(id);
            return Ok(comments);
        }

        // Add a comment to a blog post
        [HttpPost("{id}/comments")]
        [Authorize]
        public async Task<IActionResult> AddComment(string id, [FromBody] BlogCommentModel comment)
        {
           
            var user = await _userManager.FindByIdAsync(comment.UserId);
            if (user == null)
                return NotFound(new { Message = "User not found" });

            comment.Id = Guid.NewGuid().ToString();
            comment.BlogPostId = id;
            comment.ParentCommentId = id;
            comment.UserId = user.Id.ToString();
            comment.CommentDate = DateTime.UtcNow;

            await _blogRepository.AddCommentAsync(comment);
            return CreatedAtAction(nameof(GetComments), new { id }, comment);
        }

        // Delete a comment
        [HttpDelete("comments/{id}")]
        [Authorize]
        public async Task<IActionResult> DeleteComment(string id)
        {
            await _blogRepository.DeleteCommentAsync(id);
            return NoContent();
        }

    }
}
