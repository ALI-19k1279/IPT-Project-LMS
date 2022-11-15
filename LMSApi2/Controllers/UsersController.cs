﻿using LMSApi2.Authorization.AuthorizationUser;
using LMSApi2.Authorization.AuthorizationAnonymous;
using LMSApi2.DTOS.Users;
using LMSApi2.Models;
using LMSApi2.Services.Users;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using LMSApi2.Services.ClassServices;
using LMSApi2.DTOS.ClassesDTO;
using LMSApi2.Helpers;
using LMSApi2.DTOS.Announcements;
using Microsoft.Extensions.Options;
using LMSApi2.Services.FileUploadService;
using System.Net;

namespace LMSApi2.Controllers
{

    [Route("api/user/[controller]")]
    [ApiController]
    public class UsersController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly IClassService _classService;
        private readonly ILogger<User> logger;
        private readonly IOptions<AppSettings> settings;
        private readonly IFileUploadService _fileService;
        public UsersController(IUserService userService, IClassService classService, IFileUploadService fileService, ILogger<User> logger, IOptions<AppSettings> settings) {
            _userService = userService;
            this.logger = logger;
            _classService = classService;
            this.settings = settings;
            _fileService = fileService;
        }

        [Authorize]
        [HttpGet("[action]")]
        public IActionResult Authenticate() {
            
            return Ok(new { Message="Access Granted"});
        }

        [Authorize(Role.Admin)]
        [HttpGet]
        public IActionResult GetAll() {
            var users = _userService.GetAll();
            return Ok(users);

        }

        [AllowAnonymous]
        [HttpPost("[action]")]
        public IActionResult Login(AuthenticateRequest request) {
            var response = _userService.Authenticate(request);

            return Ok(response);
        }




        [HttpGet("{id}")]
        public IActionResult GetById(string id)
        {
            // only admins can access other user recordss
            var currentUser = (User)HttpContext.Items["User"];
            if (id != currentUser.UserId && currentUser.Role != Role.Admin)
                return Unauthorized(new { message = "Unauthorized" });

            var user = _userService.GetById(id);
            return Ok(user);
        }



        [Authorize]
        [HttpGet("classes")]
        public IActionResult GetById()
        {
            var currentUser = (User)HttpContext.Items["User"];
            List<ClassDTO> clss = _classService.getClassesOfUser(currentUser);
            return Ok(clss);
        }
        //[Authorize]
        [Authorize]
        [HttpGet("class/students/{id}")]
        public IActionResult ViewStudents(string id)
        {
            int cid;
            Int32.TryParse(id, out cid);
            List<User> students = _classService.getUsersEnrolledInClass(cid);
            List<UserDTO> userDTOs = new List<UserDTO>();
            foreach (User user in students)
            {
                userDTOs.Add(new UserDTO().toDTO(user));
            }



            return Ok(userDTOs);
        }


        [HttpGet("Test")]
        public IActionResult Test()
        {
            _userService.Test();
            return Ok();
        }

        [Authorize]
        [HttpPut("add/class/{code}")]
        public async Task<IActionResult> addToClass(string code)
        {
            User user = HttpContext.Items["User"] as User;
            ClassDTO _class = await _classService.addUserToClass(code, user);
            return new ObjectResult(_class) { StatusCode=(int)HttpStatusCode.Created};
        }

        [Authorize]
        [HttpGet("annoucements/class/{id}")]
        public IActionResult getAllAnnoucementsOfAClass(string id) {

            int.TryParse(id, out int cid);
            Console.WriteLine(cid);
            Console.WriteLine(_classService.isClassExists(cid));
            if (cid == 0 || cid == null || _classService.isClassExists(cid) == false)
            {
                throw new NotFoundException("no such class exists");
            }
            

            List<AnnouncementResponse> announcements = _classService.viewAnnoucements(cid);

            return Ok(announcements);
        }

        [HttpGet("/Files/{filename}")]

        public IActionResult getAnnouncementFile(string filename) {
            if (filename.Contains("/") || filename.Contains(@"\")) {
                return BadRequest();
            }
            string constructedFilePath = Path.GetFullPath(Path.Combine(settings.Value.SaveFolderPath, filename));
            if (!FileUtils.isFileExist(constructedFilePath)) {
                return NotFound();
            }
            FileStream stream = new FileStream(constructedFilePath, FileMode.Open, FileAccess.Read);
            


            return File(stream , "application/octet-stream");   
        }

        [Authorize]
        [HttpPost("upload/assignment/{id}")]
        public async  Task<IActionResult> uploadAssignment(string id , [FromForm] List<IFormFile> fileToUpload) {

            IFormCollection collection = HttpContext.Request.Form;
            User user = HttpContext.Items["User"] as User;
            int.TryParse(id, out int cid);
            if (cid == 0 || !_classService.isClassExists(cid))          //check if user enrolled in class also
            {
                throw new APIError("no such class exists");
            }
            int successfulFileUploaded = 0;
            foreach (IFormFile file in fileToUpload)
            {
                try
                {
                    await _fileService.uploadSubmissionFile(cid, file, user);
                    successfulFileUploaded++;
                }
                catch (Exception) {
                    continue;
                }
            }
            return new ObjectResult(new { Success = successfulFileUploaded, Failed = (fileToUpload.Count - successfulFileUploaded) }) { StatusCode=(int)HttpStatusCode.OK};

        }
    }
}
