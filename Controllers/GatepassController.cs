using Document_Management.Data;
using Document_Management.Hubs;
using Document_Management.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using QRCoder;
using System.Drawing;
using System.Drawing.Imaging;
using System.Text;

namespace Document_Management.Controllers
{
    public class GatepassController : Controller
    {
        //Database Context
        private readonly ApplicationDbContext _dbcontext;

        private readonly IHubContext<NotificationHub> _notificationHub;

        //Passing the dbcontext in to another variable
        public GatepassController(ApplicationDbContext context, IHubContext<NotificationHub> notification)
        {
            _dbcontext = context;
            _notificationHub = notification;
        }

        //RequestGatepass
        [HttpGet]
        public IActionResult Insert()
        {
            var username = HttpContext.Session.GetString("username");
            if (!string.IsNullOrEmpty(username))
            {
                return View();
            }
            else
            {
                return RedirectToAction("Login", "Account");
            }
        }

        //Request gatepass
        [HttpPost]
        public async Task<IActionResult> Insert(RequestGP gpInfo)
        {
            var username = HttpContext.Session.GetString("username");

            if (ModelState.IsValid)
            {
                if (username != null)
                {
                    gpInfo.Username = username;
                }

                if (gpInfo.Schedule < DateTime.Now.AddHours(2))
                {
                    TempData["error"] = "Please input a date more than 2 hours from now.";
                    return View(gpInfo);
                }

                var selectedArea = gpInfo.Area; //Market_Market

                gpInfo.Area = selectedArea.Replace("_", " "); // "Market_Market" read "_" pass to " "

                _dbcontext.Gatepass.Add(gpInfo);
                gpInfo.Status = "Pending";

                gpInfo.DateRequested = DateTime.Now;

                //Implementing the logs
                LogsModel logs = new(username, $"Requested a new gatepass in {gpInfo.Area}");
                _dbcontext.Logs.Add(logs);

                _dbcontext.SaveChanges();
                TempData["success"] = "Request created successfully";
                var hubConnections = _dbcontext.HubConnections.Where(h => h.Username == "leo").ToList();
                foreach (var hubConnection in hubConnections)
                {
                    await _notificationHub.Clients.Client(hubConnection.ConnectionId).SendAsync("ReceivedPersonalNotification", "You have a new request", gpInfo.Username);
                }
                return RedirectToAction("Insert");
            }

            return View(gpInfo);
        }

        public IActionResult Validator()
        {
            var userrole = HttpContext.Session.GetString("userrole")?.ToLower();
            if (!(userrole == "validator" || userrole == "admin"))
            {
                TempData["ErrorMessage"] = "You have no access to this action. Please contact the MIS Department if you think this is a mistake.";
                return RedirectToAction("Privacy", "Home"); // Redirect to the login page or another appropriate action
            }

            ViewBag.users = _dbcontext.Gatepass
                .OrderByDescending(user => user.Id)
                    .ToList();

            return View();
        }

        [HttpGet]
        public IActionResult Approved(int? id)
        {
            var requestGP = _dbcontext.Gatepass.FirstOrDefault(x => x.Id == id);

            if (requestGP == null)
            {
                return NotFound();
            }

            return View(requestGP);
        }

        [HttpPost, ActionName("Approved")]
        public async Task<IActionResult> Approved(int id, string approvalAction)
        {
            if (_dbcontext.Gatepass == null)
            {
                return Problem("Entity set 'ApplicationDbContext.Account' is null.");
            }

            var userrrole = HttpContext.Session.GetString("userrole")?.ToLower();
            var username = HttpContext.Session.GetString("username");

            var client = await _dbcontext.Gatepass.FindAsync(id);

            if (client != null)
            {
                if (approvalAction == "Confirm Approve")
                {
                    // Approve logic
                    client.Status = "Approved";
                    LogsModel logs = new(username, $"Approved Gatepass {client.Id}");
                    _dbcontext.Logs.Add(logs);
                    await _dbcontext.SaveChangesAsync();
                    TempData["success"] = "Approved successfully";
                    var hubConnections = _dbcontext.HubConnections.Where(h => h.Username == client.Username).ToList();
                    foreach (var hubConnection in hubConnections)
                    {
                        await _notificationHub.Clients.Client(hubConnection.ConnectionId).SendAsync("ReceivedPersonalNotification", "Your request has been approved", client.Username);
                    }
                }
                else if (approvalAction == "Confirm Disapprove")
                {
                    // Disapprove logic
                    client.Status = "Disapproved";
                    LogsModel logs = new(username, $"Disapproved Gatepass {client.Id}");
                    _dbcontext.Logs.Add(logs);
                    await _dbcontext.SaveChangesAsync();
                    TempData["success"] = "Disapproved successfully";
                    var hubConnections = _dbcontext.HubConnections.Where(h => h.Username == client.Username).ToList();
                    foreach (var hubConnection in hubConnections)
                    {
                        await _notificationHub.Clients.Client(hubConnection.ConnectionId).SendAsync("ReceivedPersonalNotification", "Your request has been disapproved", client.Username);
                    }
                }
            }

            return RedirectToAction(nameof(Validator));
        }

        [HttpGet]
        public IActionResult RecievedGP()
        {
            var username = HttpContext.Session.GetString("username");

            if (!string.IsNullOrEmpty(username))
            {
                ViewBag.users = _dbcontext.Gatepass
                    .OrderByDescending(user => user.Id)
                    .ToList();
                return View();
            }
            else
            {
                return RedirectToAction("Login", "Account");
            }
        }

        [HttpGet]
        public async Task<IActionResult> Generate(int id, RequestGP ifRead)
        {
            var requestGP = _dbcontext.Gatepass.FirstOrDefault(x => x.Id == id);

            if (requestGP == null)
            {
                return NotFound();
            }

            if (ifRead.IsRead != true)
            {
                requestGP.IsRead = true;
                await _dbcontext.SaveChangesAsync();
            }

            //int gatepassId = requestGP.GatepassId;
            string url = HttpContext.Request.Scheme + "://" + HttpContext.Request.Host + HttpContext.Request.Path + HttpContext.Request.QueryString;

            using (MemoryStream ms = new MemoryStream())
            {
                QRCodeGenerator qRCodeGenerator = new QRCodeGenerator();
                QRCodeData qRCodeData = qRCodeGenerator.CreateQrCode(url, QRCodeGenerator.ECCLevel.Q);
                QRCode qRCode = new QRCode(qRCodeData);
                using (Bitmap oBitmap = qRCode.GetGraphic(20))
                {
                    oBitmap.Save(ms, ImageFormat.Png);
                    ViewBag.QrCode = "data:image/png;base64," + Convert.ToBase64String(ms.ToArray());
                }
            }
            return View(requestGP);
        }

        [HttpPost]
        public IActionResult Generate()
        {
            return View();
        }
    }
}