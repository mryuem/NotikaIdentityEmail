using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NotikaIdentityEmail.Context;
using NotikaIdentityEmail.Entities;
using System.Linq.Expressions;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace NotikaIdentityEmail.Controllers
{
    public class CommentController : Controller
    {

        private readonly EmailContext _emailContext;
        private readonly UserManager<AppUser> _userManager;

        public CommentController(EmailContext emailContext, UserManager<AppUser> userManager)
        {
            _emailContext = emailContext;
            _userManager = userManager;
        }

        public IActionResult UserComments()
        {
            var values = _emailContext.Comments.Include(x => x.AppUser).ToList();
            return View(values);
        }
        [Authorize(Roles = "Admin")]
        public IActionResult UserCommentList()
        {
            var values = _emailContext.Comments.Include(x => x.AppUser).ToList();
            return View(values);
        }

        [HttpGet]
        public PartialViewResult CreateComment()
        {
            return PartialView();
        }

        [HttpPost]
        public async Task<IActionResult> CreateComment(Comment comment)
        {
            var user = await _userManager.FindByNameAsync(User.Identity.Name);
            comment.AppUserId = user.Id;
            comment.CommentDate = DateTime.Now;

            //Toxic Bert Api Analizi

            using (var client = new HttpClient())
            {
                var apiKey = Environment.GetEnvironmentVariable("HUGGINGFACE_API_TOKEN");

                if (string.IsNullOrWhiteSpace(apiKey))
                {
                    comment.CommentStatus = "Onay Bekliyor";
                    _emailContext.Comments.Add(comment);
                    _emailContext.SaveChanges();
                    return RedirectToAction("UserCommentList");
                }

                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

                try
                {
                    var translateRequestBody = new
                    {
                        inputs = comment.CommentDetail
                    };
                    var translationJson = JsonSerializer.Serialize(translateRequestBody);
                    var translateContent = new StringContent(translationJson, Encoding.UTF8, "application/json");
                    var translateResponse = await client.PostAsync("https://router.huggingface.co/models/Helsinki-NLP/opus-mt-tr-en", translateContent);
                    var translateResponseStrtring = await translateResponse.Content.ReadAsStringAsync();

                    string englishText = comment.CommentDetail;
                    if (translateResponseStrtring.TrimStart().StartsWith("["))
                    {
                        var translateDoc = JsonDocument.Parse(translateResponseStrtring);
                        englishText = translateDoc.RootElement[0].GetProperty("translation_text").GetString();
                    }

                    var toxicRequestBody = new
                    {
                        inputs = englishText
                    };

                    var toxicjson = JsonSerializer.Serialize(toxicRequestBody);
                    var ToxicContent = new StringContent(toxicjson, Encoding.UTF8, "application/json");
                    var ToxicResponse = await client.PostAsync("https://router.huggingface.co/hf-inference/models/unitary/toxic-bert", ToxicContent);
                    var ToxicResponseString = await ToxicResponse.Content.ReadAsStringAsync();

                    if (ToxicResponseString.TrimStart().StartsWith("["))
                    {
                        var ToxicDoc = JsonDocument.Parse(ToxicResponseString);
                        foreach (var item in ToxicDoc.RootElement[0].EnumerateArray())
                        {
                            var label = item.GetProperty("label").GetString();
                            var score = item.GetProperty("score").GetDouble();
                            if (score > 0.5)
                            {
                                comment.CommentStatus = "Toksik Yorum";
                                break;
                            }
                        }
                    }
                    if (string.IsNullOrEmpty(comment.CommentStatus))
                    {
                        comment.CommentStatus = "Yorum Onaylandı";
                    }

                }
                catch (Exception ex)
                {
                    comment.CommentStatus = "Onay Bekliyor";

                }

                _emailContext.Comments.Add(comment);
                _emailContext.SaveChanges();
                return RedirectToAction("UserCommentList");
            }
        }
    
    
        public IActionResult DeleteComment(int id)
        {
            var value = _emailContext.Comments.Find(id);
            _emailContext.Comments.Remove(value);
            _emailContext.SaveChanges();
            return RedirectToAction("UserCommentList");
        }

        public IActionResult CommentStatusToToxic(int id)
        {
            var value = _emailContext.Comments.Find(id);
            value.CommentStatus = "Toksik Yorum";
            _emailContext.SaveChanges();
            return RedirectToAction("UserCommentList");
        }
        public IActionResult CommentStatusToPassive(int id)
        {
            var value = _emailContext.Comments.Find(id);
            value.CommentStatus = "Yorum Kaldırıldı";
            _emailContext.SaveChanges();
            return RedirectToAction("UserCommentList");
        }
        public IActionResult CommentStatusToActive(int id)
        {
            var value = _emailContext.Comments.Find(id);
            value.CommentStatus = "Yorum Onaylandı";
            _emailContext.SaveChanges();
            return RedirectToAction("UserCommentList");
        }
    }

}