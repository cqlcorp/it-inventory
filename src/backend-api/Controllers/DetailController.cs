using backend_api.Helpers;
using backend_api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.DirectoryServices.AccountManagement;
using System.Linq;

namespace backend_api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DetailController : ContextController
    {
        public DetailController(ITInventoryDBContext context) : base(context) { }

        /* GET: api/detail/{model}/{id}
         *      Return: A json for the specific model for each id. See before for specifics.
         */
        [HttpGet]
        [Route("{model}/{id}")]
        public IActionResult GetDetail([FromRoute] string model, int id)
        {
            model = VerbatimMatch(model);

            switch (model)
            {
                case "employee":
                    return GetEmployeeDetail(id);
                case "program":
                    return GetProgramDetail(id);
                case "department":
                    return GetDepartmentDetail(id);
                case "server":
                    return GetServerDetail(model, id);
                case "computer":
                    return GetComputerDetail(model, id);
                case "monitor":
                    return GetMonitorDetail(model, id);
                case "peripheral":
                    return GetPeripheralDetail(model, id);
                default:
                    return BadRequest("Invalid Model");
            }

        }

        /* GET: api/detail/ProgramOverview/{program}/{archived}
         * Function returns the program overview information   
         * If archived is true then the overview is for programs where isDeleted is true
         * Returns:{
         *          ProgramOverview:{
         *              icon: string,
         *              CountOfProgramsInUse: int,
         *              CountOfProgramsOverall: int
         *              Program(name) : string,
         *              IsLicense : bool,
         *              ProgramCostFlatCost : int or null,
         *              ProgramCostPerYear : int or null,
         *              isCostPerYear : bool,
         *              ProgramLicenseKey(if Admin) : string or null
         *          },
         *          IndividualPrograms : [
         *          {
         *              programID : int,
         *              EmployeeName, string,
         *              EmployeeID : int,
         *              ProgramLicenseKey(if admin) : string or null
         *              Program Renewal date : dateTime
         *          ] for all the individual programs that are part of the program overview
         *          ListOfPlugins: [
                    {
                        "pluginName": string,
                        "renewalDate": dateTime,
                        "pluginFlatCost": int,
                        "pluginCostPerYear": int,
                        "isCostPerYear": bool
                    ] for the plugins of the program overview
                    },
         *          
         *                          
         */
        [HttpGet]
        [Route("ProgramOverview/{program}/{archived}")]
        public IActionResult GetProgramOverview([FromRoute] bool archived, [FromRoute] string program)
        {
            // Holds the license key of the program overview if they are all the same.
            string ProgramLicenseKey = null;
            //lambda to get the id of any of the first program with that name
            int id = _context.Program.Where(x => x.ProgramName == program).Select(x => x.ProgramId).FirstOrDefault();

            // Return not found if id is the default value.
            if (id == 0)
            {
                return NotFound();
            }
            else
            {
                //creating string icon
                string icon = $"/image/program/{id}";

                // list of all programs that are not deleted
                var UsefulProgramsList = _context.Program.Where(x => x.IsDeleted == archived && x.ProgramName == program);

                // calculate the count of programs under this specific distinct program name that are in use
                var CountProgInUse = UsefulProgramsList.Where(x => x.ProgramName == program && x.EmployeeId != null && x.IsDeleted == false).Count();

                // calculate the count of programs under this specific distinct program name
                var CountProgOverall = UsefulProgramsList.Where(x => x.ProgramName == program).Count();

                // calculate the cost of each distinct program if it is charged yearly 
                var ProgCostPerYear = _context.Program.Where(x => x.ProgramName == program && x.ProgramCostPerYear != null && x.IsDeleted != true).Sum(x => x.ProgramCostPerYear);

                // calculate the cost of each distinct program if it is charged as a flat rate 
                var ProgFlatCost = _context.Program.Where(x => x.ProgramName == program && x.ProgramFlatCost != null && x.IsDeleted != true).Sum(x => x.ProgramFlatCost);

                // This lambda returns true if all the license keys are the same from the current program
                var LicenseKeySame = !(_context.Program.Where(x => x.ProgramName == program && x.IsDeleted == false).ToList().Any(x => x.ProgramLicenseKey != _context.Program.ToList().First().ProgramLicenseKey));

                // if all the license keys are the same then find the license key that they all are
                if (LicenseKeySame == true)
                    ProgramLicenseKey = UsefulProgramsList.Select(x => x.ProgramLicenseKey).FirstOrDefault();

                // Lambda to collect all the ids of the programs that belong this program overview
                var programIds = _context.Program.Where(x => x.ProgramName == program).Select(x => x.ProgramId).ToList();

                // list to hold the individual programs from the program overview
                List<object> inDivPrograms = new List<object>();

                // check if they all have a purchase date
                bool havePurchaseDate = UsefulProgramsList.All(x => x.DateBought != null) ? true : false;
                bool samePurchaseDate = havePurchaseDate; // if they don't all have a purchase date, they can't all match
                // grab a purchase date value and hang onto it for comparison purposes
                var purchaseDate = UsefulProgramsList.Select(x => x.DateBought).FirstOrDefault();

                // check if they all have a renewal date
                bool haveRenewalDate = UsefulProgramsList.All(x => x.RenewalDate != null) ? true : false;
                bool sameRenewalDate = haveRenewalDate; // if they don't all have a purchase date, they can't all match
                // grab a renewal date value and hang onto it for comparison purposes
                var renewalDate = UsefulProgramsList.Select(x => x.RenewalDate).FirstOrDefault();

                // check if they all have a purchase link
                bool havePurchaseLink = UsefulProgramsList.All(x => x.ProgramPurchaseLink != "") ? true : false;
                bool samePurchaseLink = haveRenewalDate; // if they don't all have a purchase date, they can't all match
                // grab a purchase link value and hang onto it for comparison purposes
                var purchaseLink = UsefulProgramsList.Select(x => x.ProgramPurchaseLink).FirstOrDefault();

                // check if they all have a description
                bool haveDescription = UsefulProgramsList.All(x => x.Description != "") ? true : false;
                // grab a description value and hang onto it for comparison purposes
                var progDescrip = UsefulProgramsList.Select(x => x.Description).FirstOrDefault();
                bool sameDescription = true;

                //loop through all the individual programs that are under of the current overview program
                foreach (var prog in UsefulProgramsList)
                {
                    // see if the program's description matches the others (but don't bother if there's already been a mismatch)
                    if (sameDescription == true) {
                        if (prog.Description != progDescrip) {
                            sameDescription = false;
                        }
                    }
                    // check if purchase date matches the others
                    if (samePurchaseDate == true) {
                        if (prog.DateBought != purchaseDate) {
                            samePurchaseDate = false;
                            purchaseDate = null;
                        }
                    }
                    // check if renewal date matches the others
                    if (sameRenewalDate == true) {
                        if (prog.RenewalDate != renewalDate) {
                            sameRenewalDate = false;
                            renewalDate = null;
                        }
                    }
                    // check if purchase link matches the others
                    if (samePurchaseLink == true) {
                        if (prog.ProgramPurchaseLink != purchaseLink) {
                            samePurchaseLink = false;
                            purchaseLink = "";
                        }
                    }

                    // holds the employee name for concatenation purposes 
                    var employeeName = "";

                    // holds the employee id so the front end can set up click-ability 
                    int employeeId = -1;
                    // Concatenating employees first and last name of the employee who owns the program if the program is assigned
                    // and if the program is not deleted
                    // finding employee id
                    if (prog.EmployeeId != null && prog.IsDeleted == false)
                    {
                        var empFirst = _context.Employee.Where(x => x.EmployeeId == prog.EmployeeId && x.IsDeleted == false).Select(x => x.FirstName).FirstOrDefault();
                        var empLast = _context.Employee.Where(x => x.EmployeeId == prog.EmployeeId && x.IsDeleted == false).Select(x => x.LastName).FirstOrDefault();
                        employeeName = empFirst + " " + empLast;
                        employeeId = _context.Employee.Where(x => x.EmployeeId == prog.EmployeeId && x.IsDeleted == false).Select(x => x.EmployeeId).FirstOrDefault();
                    }
                    // Creating the list of individual programs with the necessary returnables. 
                    // Only returning license key if the account that hits the endpoint is an admin.
                    inDivPrograms.Add(new
                    {
                        prog.ProgramId,
                        employeeName,
                        employeeId,
                        ProgramlicenseKey = isAdmin() ? prog.ProgramLicenseKey : null,
                        prog.RenewalDate
                    });
                }
                // lambda to check if all the indiv programs under this name are a license
                bool isLicense = UsefulProgramsList.All(x => x.IsLicense == true) ? true : false;

                var programDescription = "";
                // if all the descriptions match, save that description in a var to return later
                if (sameDescription == true) {
                    programDescription = progDescrip;
                }

                // get the number of months per renewal
                var monthsPerRenewal = UsefulProgramsList.Select(x => x.MonthsPerRenewal).FirstOrDefault();

                // creating a list of plug-ins that will be returned
                List<object> ListOfPlugins = new List<object>();

                // loop through every plug-in and if they are a plug-in of the current overview program add all the info that we need about them
                // to the list of plug-ins
                foreach (var plugin in _context.Plugins.Where(x => (!x.IsDeleted)))
                {
                    if (programIds.Contains(plugin.ProgramId))
                    {
                        ListOfPlugins.Add(new
                        {
                            plugin.PluginId,
                            plugin.TextField,
                            plugin.MonthsPerRenewal,
                            plugin.DateBought,
                            plugin.PluginName,
                            plugin.RenewalDate,
                            plugin.PluginFlatCost,
                            plugin.PluginCostPerYear,
                            plugin.IsCostPerYear
                        });
                    }
                }

                // Creating the list of returnables that is for the program overview page. 
                // Again license key is only returned to an authorized user.
                var programOverview = new
                {
                    icon,
                    CountProgInUse,
                    CountProgOverall,
                    program,
                    isLicense,
                    ProgFlatCost,
                    ProgCostPerYear,
                    UsefulProgramsList.FirstOrDefault().IsCostPerYear,
                    ProgramlicenseKey = isAdmin() ? ProgramLicenseKey : null,
                    programDescription,
                    monthsPerRenewal,
                    purchaseDate,
                    renewalDate,
                    purchaseLink,
                };
                // returning the amalgamation of the various returnables into a nice JSON object :)
                var ProgramOverViewPage = new { programOverview, inDivPrograms, ListOfPlugins };
                return Ok(new List<object> { ProgramOverViewPage });
            }

        }
        /*
         * GET: api/detail/employee/{id}
         * Function returns the employee detail information.
         * Returns: [ {
         *      isDeleted: boolean,
         *      email: string,
         *      picture: partial URL (as string),
         *      totalProgramCostPerMonth: decimal,
         *      totalHardwareCost: decimal,
         *      firstName: string,
         *      lastName: string,
         *      department: string,
         *      departmentID: number,
         *      role: string,
         *      hireDate: date (as string),
         *      archiveDate: Date (as string),
         *      isAdmin : bool,
         *      TextField: string,
         *      hardware: [ {
         *          id: int,
         *          type: string,
         *          clickable : string
         *          make: string,
         *          model: string,
         *          serialNumber: string,
         *          mfg: string,
         *          purchaseDate: date (as string),
         *          tooltip: {
         *              ...
         *              (assortedValues as strings or ints)
         *          },
         *      } ,.. ],
         *      software: [ {
         *          id: int,
         *          name: string,
         *          licenseKey: string, //TODO
         *          costPerMonth: decimal,
         *          flatCost: decimal,
         *      } ,.. ],
         *      licenses: [ {
         *          id: int,
         *          name: string,
         *          licenseKey: string, //TODO
         *          costPerMonth: decimal,
         *          flatCost: decimal,
         *          LicensesCount : int
         *      } ,.. ],
         *  } ]           
         */
        private IActionResult GetEmployeeDetail(int id)
        {
            bool isAdmin = this.isAdmin();

            // Find the requested employee
            var emp = _context.Employee.Find(id);
            if (emp == null)
            {
                return NotFound();
            }
            else
            {
                // Initialize return elements.
                decimal totalHardwareCost = 0.0m;
                List<object> hardware = new List<object>();

                // For every server the employee is assigned to, create custom objects to return and add to the hardware list.
                foreach (Server sv in _context.Server.Where(server => server.EmployeeId == id && !server.IsDeleted))
                {
                    object tooltip = new
                    {
                        sv.NumberOfCores,
                        sv.Ram,
                        sv.LocalHHD,
                    };
                    object server = new
                    {
                        id = sv.ServerId,
                        type = nameof(Server),
                        clickable = nameof(Server) + "/" + sv.ServerId,
                        sv.Make,
                        sv.Model,
                        sv.SerialNumber,
                        sv.MFG,
                        sv.PurchaseDate,
                        tooltip,
                    };
                    hardware.Add(server);
                    totalHardwareCost += sv.FlatCost ?? 0.0m;
                }

                // For every computer the employee is assigned to, create custom objects to return and add to the hardware list.
                foreach (Computer cp in _context.Computer.Where(computer => computer.EmployeeId == id && !computer.IsDeleted))
                {
                    object tooltip = new
                    {
                        cp.Cpu,
                        cp.Ramgb,
                        cp.Ssdgb,
                    };
                    object computer = new
                    {
                        id = cp.ComputerId,
                        type = nameof(Computer),
                        clickable = nameof(Computer) + "/" + cp.ComputerId,
                        cp.Make,
                        cp.Model,
                        cp.SerialNumber,
                        cp.MFG,
                        cp.PurchaseDate,
                        tooltip,
                    };
                    hardware.Add(computer);
                    totalHardwareCost += cp.FlatCost ?? 0.0m;
                }

                // For every monitor the employee is assigned to, create custom objects to return and add to the hardware list.
                foreach (Monitor mn in _context.Monitor.Where(monitor => monitor.EmployeeId == id && !monitor.IsDeleted))
                {
                    object tooltip = new
                    {
                    };
                    object monitor = new
                    {
                        id = mn.MonitorId,
                        type = nameof(Monitor),
                        clickable = nameof(Monitor) + "/" + mn.MonitorId,
                        mn.Make,
                        mn.Model,
                        mn.SerialNumber,
                        mn.MFG,
                        mn.PurchaseDate,
                        tooltip,
                    };
                    hardware.Add(monitor);
                    totalHardwareCost += mn.FlatCost ?? 0.0m;
                }

                // For every peripheral the employee is assigned to, create custom objects to return and add to the hardware list.
                foreach (Peripheral pr in _context.Peripheral.Where(peripheral => peripheral.EmployeeId == id && !peripheral.IsDeleted))
                {
                    // NOTE: Peripheral does not have make and model, but instead has name and type.
                    object tooltip = new
                    {
                    };
                    object peripheral = new
                    {
                        id = pr.PeripheralId,
                        type = nameof(Peripheral),
                        clickable = nameof(Peripheral) + "/" + pr.PeripheralId,
                        make = pr.PeripheralName,
                        model = pr.PeripheralType,
                        pr.SerialNumber,
                        pr.MFG,
                        pr.PurchaseDate,
                        tooltip,
                    };
                    hardware.Add(peripheral);
                    totalHardwareCost += pr.FlatCost ?? 0.0m;
                }

                // Initialize elements to return for programs.
                List<object> software = new List<object>();
                List<object> licenses = new List<object>();
                decimal totalProgramCostPerMonth = 0.0m;

                // For each program that is not deleted and is assigned to the employee, create an object to add to the list.
                foreach (Models.Program prog in _context.Program.Where(prog => !prog.IsDeleted && prog.EmployeeId == id))
                {
                    decimal costPerMonth = prog.ProgramCostPerYear / 12 ?? 0.0m;
                    costPerMonth = Math.Round(costPerMonth, 2, MidpointRounding.ToEven);
                    totalProgramCostPerMonth += costPerMonth;

                    if (prog.IsLicense)
                    {
                        object license = new
                        {
                            id = prog.ProgramId,
                            name = prog.ProgramName,
                            licensesKey = isAdmin ? prog.ProgramLicenseKey : null,
                            costPerMonth,
                            flatCost = prog.ProgramFlatCost,
                            licensesCount = 1
                        };
                        licenses.Add(license);
                    }
                    // If not a license, then a software.
                    else
                    {
                        object sw = new
                        {
                            id = prog.ProgramId,
                            name = prog.ProgramName,
                            licenseKey = isAdmin ? prog.ProgramLicenseKey : null,
                            costPerMonth,
                            flatCost = prog.ProgramFlatCost
                        };
                        software.Add(sw);
                    }
                }

                // Partial path for picture
                string picture = $"/image/employee/{id}";

                // Get the department name
                var department = _context.Department
                    .Where(dep => dep.DepartmentId == emp.DepartmentID)
                    .Where(dep => dep.IsDeleted == false)
                    .FirstOrDefault().DepartmentName;

                // list that will hold the unassigned hardware
                List<object> UnassignedHardware = new List<object>();

                UnassignedHardware.AddRange(UnassignedHardwareHelper<Monitor>());
                UnassignedHardware.AddRange(UnassignedHardwareHelper<Server>());
                UnassignedHardware.AddRange(UnassignedHardwareHelper<Computer>());
                UnassignedHardware.AddRange(UnassignedHardwareHelper<Peripheral>());

                // Unassigned programs lists for returning purposes
                List<object> UnassignedSoftware = new List<object>();
                List<object> UnassignedLicenses = new List<object>();

                // loop and lambda to find all the distinct programs that have any of their individual programs unassigned and loop though them
                foreach (var prog in _context.Program.Where(x => x.EmployeeId == null && x.IsDeleted == false).GroupBy(prog => prog.ProgramName).Select(x => x.FirstOrDefault()).ToList())
                {
                    // for the licenses list
                    if (prog.IsLicense == false)
                    {
                        var SW = new
                        {
                            prog.ProgramName,
                            prog.ProgramId,
                            type = nameof(Program)
                        };
                        UnassignedSoftware.Add(SW);
                    }
                    // for the software list
                    else
                    {
                        var license = new
                        {
                            prog.ProgramName,
                            prog.ProgramId,
                            type = nameof(Program)
                        };
                        UnassignedLicenses.Add(license);
                    }
                }

                // bool to store whether this current employee is an Admin
                bool Admin = false;
                using (var adContext = new PrincipalContext(ContextType.Domain, "CQLCORP"))
                {
                    var user = UserNameHelper(emp.FirstName, emp.LastName);
                    if (user != null)
                    {
                        // Return the isAdmin field from the AuthIDServer matching the Guid.
                        Admin = _context.AuthIdserver
                            .Where(x => x.ActiveDirectoryId == user.Guid.Value)
                            .First().IsAdmin;
                    }
                    else
                    {
                        Admin = false;
                    }
                }


                // Combine it all into a nice JSON :)
                object employeeDetail = new
                {
                    emp.IsDeleted,
                    emp.Email,
                    picture,
                    totalProgramCostMonthly = Math.Round(totalProgramCostPerMonth, 2, MidpointRounding.ToEven),
                    totalHardwareCost,
                    emp.FirstName,
                    emp.LastName,
                    department,
                    emp.DepartmentID,
                    emp.Role,
                    emp.HireDate,
                    emp.ArchiveDate,
                    Admin,
                    emp.TextField,
                    hardware,
                    software,
                    licenses,
                    UnassignedHardware,
                    UnassignedSoftware,
                    UnassignedLicenses,
                };
                return Ok(new List<object> { employeeDetail });
            }
        }
        /*
        * GET: api/detail/program/{id}
        * Function returns the program detail information.
        * Returns: {
        *    "isDeleted": boolean,
        *    "programName": string,
        *    "picture: partial URL (as string),
        *    "renewalDate": date,
        *    "dateBought": date,
        *    "employeeName": string,
        *    "employeeID : int
        *    "progHistory": [
        *           {
        *        "programHistoryId": int,
        *        "currentOwnerId": int,
        *        "currentOwnerStartDate": date,
        *        "previousOwnerId": int,
        *        "programId": int,
        *        "eventName": string,
        *        "eventDescription": string
        *               ] for the events of this specific program
        *           }  
        *    "programCostPerYear": int,
        *    "programFlatCost": int,
        *    "isCostPerYear": bool,
        *    "description": string,
        *    "programPurchaseLink": string,
        *    "List of employees" : [
        *       "EmployeeName : string,
        *       "EmployeeID : int
        *    ]
        *   }
        * 
        * 
        */
        private IActionResult GetProgramDetail(int id)
        {
            //finding the program
            var prog = _context.Program.Find(id);
            // checking if the program actually exists and isn't deleted
            if (prog == null )
            {
                return NotFound();
            }
            // if the program does exist...
            else
            {
                // this accesses the ID of the first program with the same name which is where we store the photo
                var ID = _context.Program
                    .Where(x => x.ProgramName == prog.ProgramName)
                    .Select(x => x.ProgramId)
                    .FirstOrDefault();

                // Partial path for picture
                string picture = $"/image/program/{ID}";


                // holds the employee name for concatenation purposes 
                var employeeName = "";
                int employeeId = -1;
                // Concatenating employees first and last name of the employee who owns the program if the program is assigned

                if (prog.EmployeeId != null)
                {
                    var empFirst = _context.Employee
                        .Where(x => x.EmployeeId == prog.EmployeeId && x.IsDeleted == false)
                        .Select(x => x.FirstName)
                        .FirstOrDefault();

                    var empLast = _context.Employee
                        .Where(x => x.EmployeeId == prog.EmployeeId && x.IsDeleted == false)
                        .Select(x => x.LastName)
                        .FirstOrDefault();

                    employeeId = _context.Employee
                        .Where(x => x.EmployeeId == prog.EmployeeId && x.IsDeleted == false)
                        .Select(x => x.EmployeeId)
                        .FirstOrDefault();

                    employeeName = empFirst + " " + empLast;
                }
                List<object> entries = new List<object>();
                // find all the events/history of the current program
                foreach (var entry in _context.ProgramHistory.Where(x => x.ProgramId == prog.ProgramId))
                {
                    var empFirst = _context.Employee
                        .Where(x => x.EmployeeId == entry.EmployeeId)
                        .Select(x => x.FirstName)
                        .FirstOrDefault();

                    var empLast = _context.Employee
                        .Where(x => x.EmployeeId == entry.EmployeeId)
                        .Select(x => x.LastName)
                        .FirstOrDefault();

                    var employeeNameHistory = empFirst + " " + empLast;

                    var singleEntry = new
                    {
                        employeeNameHistory,
                        entry.EventType,
                        entry.EventDate,
                        historyId = entry.ProgramHistoryId
                    };
                    entries.Add(singleEntry);
                }

                // Returning the details of the program into a nice JSON object :)
                var ProgramDetails = new
                {
                    prog.IsDeleted,
                    prog.ProgramId,
                    prog.ProgramName,
                    picture,
                    prog.RenewalDate,
                    prog.DateBought,
                    prog.MonthsPerRenewal,
                    employeeName,
                    employeeId = employeeName != "" ? employeeId : -1,
                    entries,
                    ProgramLicenseKey = isAdmin() ? prog.ProgramLicenseKey : null,
                    prog.ProgramCostPerYear,
                    prog.ProgramFlatCost,
                    prog.IsCostPerYear,
                    prog.Description,
                    prog.ProgramPurchaseLink,
                    prog.HasPlugIn,
                    listOfEmployees = ListOfEmployees()
                };

                return Ok(new List<object> { ProgramDetails });
            }

        }

        /*
       * GET: api/detail/department/{id}
       * Function returns the program detail information.
       * Returns : {
       *    "isDeleted": boolean,
       *    "departmentName": String,
       *    "totalCostOfActHardwareInDep": decimal,
       *    "totalCostOfProgramsInDep": decimal,
       *    "picture: partial URL (as string),
       *    "countempsIDsInDep": int,
       *    "jsonHardware": {
       *        DefaultHardware [
       *        ] list with all the default hardware
       *     },
       *     "jsonSoftware": {
       *        DefaultSoftware [
       *        ] list with all the default software
       *     },
       *     "listOfEmployees": [
       *        {
       *             "employeeID : int,
       *             "employeeName": string,
       *             "hireDate": date,
       *             "hardwareCostForEmp": decimal,
       *             "programCostForEmp": decimal
       *         },
       *     ], For all the employees in this department
       *      "listOfTablePrograms": [
       *         {
       *             "programName": string,
       *             "programCount": int,
       *             "programCostPerYear": decimal,
       *             "programIsCostPerYear": bool
       *         }, 
       *     ], For all the programs that owned by this department that are not licenses,
       *     "licensesList": [
       *         {
       *         "progName": string,
       *         "countOfThatLicense": int
       *         },
       *     ] For all the programs that owned by this department that are licenses
       * }
       */
        private IActionResult GetDepartmentDetail(int DepId)
        {

            //finding the department
            var dep = _context.Department.Find(DepId);
            // checking if the department actually exists and isn't deleted
            if (dep == null )
            {
                return NotFound();
            }
            // if the department does exist...
            else
            {
                // storing the partial picture url
                string picture = $"/image/department/{DepId}";

                //Cost of Programs per department value
                decimal? TotalCostOfProgramsInDep = 0.0m;

                // cost of hardware per department value
                decimal? TotalCostOfActHardwareInDep = 0.0m;

                // lambda to collect all the employees in the current department into a list
                var empsInDep = _context.Employee
                    .Where(x => x.DepartmentID == DepId && x.IsDeleted == false);

                // lambda to get the ids of the all the employees in the current department
                var empsIDsInDep = empsInDep
                    .Select(x => x.EmployeeId)
                    .ToList();

                // lambda to count the number of employees in the current department
                var CountEmpsInDep = empsInDep.Count();

                // Need to qualify Program with Models
                // so it does not conflict with Program.cs that runs the program.
                List<Models.Program> programsOfEmpsInDepartment = new List<Models.Program>();


                // Make sure the program is not deleted and the employeeID is not null
                _context.Program
                    .Where(x => x.IsDeleted == false)
                    .Where(x => x.EmployeeId != null)
                    .ToList()
                    .ForEach(x =>
                    {
                        if (empsIDsInDep.Contains(x.EmployeeId.Value))
                        {
                            programsOfEmpsInDepartment.Add(x);
                        }
                    }
                    );

                // lambda to calculate the total cost of the programs in the current department
                TotalCostOfProgramsInDep = programsOfEmpsInDepartment
                    .Where(x => x.ProgramCostPerYear != null)
                    .Sum(x => x.ProgramCostPerYear);

                // loop to calculate the cost of monitors that employees from the current department are accumulating 
                TotalCostOfActHardwareInDep += CalculatedHardwareCost<Monitor>(empsIDsInDep);
                TotalCostOfActHardwareInDep += CalculatedHardwareCost<Computer>(empsIDsInDep);
                TotalCostOfActHardwareInDep += CalculatedHardwareCost<Peripheral>(empsIDsInDep);
                TotalCostOfActHardwareInDep += CalculatedHardwareCost<Server>(empsIDsInDep);



                // list of employees that will hold the info for the employees list that on the table as specified in the method comment header
                var ListOfEmployees = new List<object>();


                // loop through all the employees and find how much they are costing individually costing in their programs and hardware
                foreach (var emp in empsInDep)
                {
                    decimal? HardwareCostForEmp = 0.0m;
                    // Sum the costs of all the computers owned by the current employee where the computer is not deleted and the cost is not null
                    HardwareCostForEmp += CalculatedHardwareCostForEmp<Monitor>(emp.EmployeeId);
                    HardwareCostForEmp += CalculatedHardwareCostForEmp<Computer>(emp.EmployeeId);
                    HardwareCostForEmp += CalculatedHardwareCostForEmp<Peripheral>(emp.EmployeeId);
                    HardwareCostForEmp += CalculatedHardwareCostForEmp<Server>(emp.EmployeeId);

                    // Sum the costs of all the programs that are charged as cost per year owned by the current employee where the program is not deleted and the cost is not null
                    var ProgCostForEmpPerYear = _context.Program
                        .Where(x => x.EmployeeId == emp.EmployeeId && x.ProgramCostPerYear != null && x.IsDeleted != true)
                        .Sum(x => x.ProgramCostPerYear);

                    // Dividing the yearly cost into months Adding the programs costs into one variable if the values are not null
                    decimal ProgramCostForEmp = Math.Round(System.Convert.ToDecimal(ProgCostForEmpPerYear / 12), 2, MidpointRounding.ToEven);

                    // concatenating the first and the last name
                    var EmployeeName = emp.FirstName + " " + emp.LastName;

                    // building employee object
                    var Employee = new
                    {
                        emp.EmployeeId,
                        EmployeeName,
                        emp.HireDate,
                        HardwareCostForEmp,
                        ProgramCostForEmp
                    };

                    ListOfEmployees.Add(Employee);
                }



                // Make a list of the distinct programs of the employees
                // in the department.
                var distinctPrograms = programsOfEmpsInDepartment
                    .Where(x => x.IsLicense == false)
                    .GroupBy(prog => prog.ProgramName)
                    .Select(name => name.FirstOrDefault())
                    .Select(program => program.ProgramName);

                // Create a list with name, count, costPerYear containing the unique programs in the department
                List<DepartmentTableProgram> listOfTablePrograms = new List<DepartmentTableProgram>();
                foreach (var name in distinctPrograms)
                {
                    // Construct a new object to be added to the list.
                    listOfTablePrograms.Add(new DepartmentTableProgram(name, 0, 0.0m, true));
                }

                // Aggregate the programs in the department that are the same name.
                // Count the programs and add the cost.
                foreach (Models.Program departmentProgram in programsOfEmpsInDepartment.Where(x => x.IsLicense == false))
                {
                    // The index of the unique program that has the same name as the employee's program in the department
                    int index = listOfTablePrograms.FindIndex(uniqueProgram => uniqueProgram.ProgramName == departmentProgram.ProgramName);
                    if (index >= 0)
                    {
                        listOfTablePrograms[index].ProgramCount += 1;
                        // ?? operator to make sure CostPerYear is not null. If it is, add 0.
                        listOfTablePrograms[index].ProgramCostPerYear += departmentProgram.ProgramCostPerYear ?? 0.0m;
                        listOfTablePrograms[index].ProgramIsCostPerYear = departmentProgram.IsCostPerYear ? true : false;
                    }
                }

                // lambda to find the distinct licenses from all the programs
                var distinctLicensePrograms = programsOfEmpsInDepartment
                    .Where(x => x.IsLicense == true)
                    .GroupBy(prog => prog.ProgramName)
                    .Select(name => name.FirstOrDefault())
                    .Select(program => program.ProgramName)
                    .ToList();

                // list that will contain the licenses and how many licenses this current department is using
                List<object> LicensesList = new List<object>();

                // loop though distinct licenses name and count how many programs that belong to this current have that specific name
                foreach (var progName in distinctLicensePrograms)
                {
                    var CountOfThatLicense = programsOfEmpsInDepartment
                        .Where(x => x.IsLicense == true && x.ProgramName == progName)
                        .Count();

                    // creating license object that contains the necessary returnables.
                    var License = new
                    {
                        progName,
                        CountOfThatLicense
                    };
                    LicensesList.Add(License);
                }
                // pull stringifyed default hardware and software out into a nice JSON object :) using JSON package.
                // if deparment is "Utilities" or "Unassigned", ignore this

                JObject jsonHardware = null;
                JObject jsonPrograms = null;
                if (dep.DepartmentName != "Utilities" && dep.DepartmentName != "Unassigned")
                {
                    jsonHardware = JObject.Parse(dep.DefaultHardware);
                    jsonPrograms = JObject.Parse(dep.DefaultPrograms);
                }

                // creating list of necessary returnables that are specified in the method comment header
                var DepartmentDetailPage = new
                {
                    dep.IsDeleted,
                    dep.DepartmentName,
                    TotalCostOfActHardwareInDep,
                    TotalCostOfProgramsInDep,
                    picture,
                    CountEmpsInDep,
                    DefaultHardware = jsonHardware != null ? jsonHardware["DefaultHardware"] : null,
                    DefaultLicenses = jsonPrograms != null ? jsonPrograms["license"] : null,
                    DefaultSoftware = jsonPrograms != null ? jsonPrograms["software"] : null,
                    ListOfEmployees,
                    listOfTablePrograms,
                    LicensesList
                };
                return Ok(new List<object> { DepartmentDetailPage });
            }
        }

        /* GET: api/detail/server/{id}
         * Function returns the server detail information.
         * Return: 
          {
                "server": {
                    "serverId": int,
                    "fqdn": string,
                    "numberOfCores": int,
                    "operatingSystem": string,
                    "ram": int,
                    "virtualize": bool,
                    "renewalDate": date (as string),
                    "employeeId": int,
                    "purchaseDate": date (as string),
                    "flatCost": decimal,
                    "endOfLife": date (as string),
                    "isAssigned": bool,
                    "textField": string,
                    "costPerYear": decimal,
                    "isDeleted": bool,
                    "mfg": string,
                    "make": string,
                    "model": string,
                    "ipAddress": string,
                    "san": string,
                    "localHHD": string,
                    "location": string,
                    "serialNumber": string,
                },                
                "isDeleted": boolean,
                "departmentName : string,
                "departmentID : int,
                "icon": partial URL (as string),
                "serverClicked" : string,
                "employeeAssignedName": string,
                "employeeAssignedId": number,

                // TODO: update this comment
                "serverHistory": [
                    {
                        "hardwareHistoryId": int,
                        "currentOwnerId": int,
                        "currentOwnerStartDate": date (as string),
                        "previousOwnerId": int,
                        "hardwareType": string,
                        "hardwareId": int,
                        "eventName": string,
                        "eventDescription": string,
                    },
                ]
            }
         */
        private IActionResult GetServerDetail(string model, int serverID)
        {
            // Find the requested server
            var sv = _context.Server.Find(serverID);
            if (sv == null )
            {
                return NotFound();
            }
            else
            {
                // Partial image string
                var icon = $"/image/server/{serverID}";

                // Employee the server is assigned to.
                var employeeAssigned = _context.Employee
                    .Where(x => x.EmployeeId == sv.EmployeeId)
                    .FirstOrDefault();

                // int to hold the department Id for click-ability. -1 is the default if hardware is not assigned. 
                int departmentID = -1;
                // string to hold department name. empty string if hardware is unassigned. 
                string departmentName = "";

                // if an employee is assigned to this hardware then find their department
                if (employeeAssigned != null)
                {
                    var dep = _context.Department
                        .Where(x => x.DepartmentId == employeeAssigned.DepartmentID)
                        .FirstOrDefault();

                    departmentID = dep.DepartmentId;
                    departmentName = dep.DepartmentName;
                }

                // Server History
                List<object> ServerHistory = new List<object>();

                // Formatting the data returned of this piece of hardware's history and adding it to a list.
                foreach (var entry in _context.HardwareHistory.Where(x => x.HardwareType.ToLower() == model && x.HardwareId == serverID))
                {
                    var empFirst = _context.Employee
                        .Where(x => x.EmployeeId == entry.EmployeeId)
                        .Select(x => x.FirstName)
                        .FirstOrDefault();

                    var empLast = _context.Employee
                        .Where(x => x.EmployeeId == entry.EmployeeId)
                        .Select(x => x.LastName)
                        .FirstOrDefault();

                    var employeeName = empFirst + " " + empLast;

                    var singleEntry = 
                        new {
                            employeeName,
                            entry.EventType,
                            entry.EventDate,
                            historyId = entry.HardwareHistoryId
                        };
                    ServerHistory.Add(singleEntry);
                }

                var serverClicked = nameof(Server) + "/" + sv.ServerId;

                // list to hold the list of employees that are not deleted so the front end can assign programs to individuals 

                var serverDetailPage = (new
                {
                    sv.IsDeleted,
                    server = sv,
                    departmentName,
                    departmentID,
                    icon,
                    serverClicked,
                    employeeAssignedName = employeeAssigned != null ? employeeAssigned.FirstName + " " + employeeAssigned.LastName : "",
                    employeeAssignedId = employeeAssigned != null ? employeeAssigned.EmployeeId: -1,
                    ServerHistory,
                    listOfEmployees = ListOfEmployees()
                });
                return Ok(new List<object> { serverDetailPage });
            }
        }

        /*
         * GET: api/detail/computer/{id}
            * Function returns the computer detail information.
            * Returns : {
            *    "computer": {
                        "serverId": int,
                        "fqdn": string,
                        "numberOfCores": int,
                        "operatingSystem": string,
                        "ram": int,
                        "virtualize": bool,
                        "renewalDate": date,
                        "employeeId": int,
                        "purchaseDate": date,
                        "flatCost": int,
                        "endOfLife": date,
                        "isAssigned": bool,
                        "textField": string,
                        "costPerYear": int,
                        "isDeleted": bool,
                        "mfg": string,
                        "make": string,
                        "model": string,
                        "ipAddress": string,
                        "san": string,
                        "localHHD": string,
                        "location": string,
                        "serialNumber": string
                    },                    
                    "isDeleted": boolean,
                    "departmentName : string,
                    "departmentID : int,
                    "icon": partial URL (as string),
                    "computerClicked" : string,
                    "employeeAssignedName": string,
                    "employeeAssignedId": number,
                    "compHistory": [
                        {
                        "hardwareHistoryId": int,
                        "currentOwnerId": int,
                        "currentOwnerStartDate": string,
                        "previousOwnerId": int,
                        "hardwareType": string,
                        "hardwareId": int,
                        "eventName": string,
                        "eventDescription": string
                        }
                    ]
                }
       */
        private IActionResult GetComputerDetail(string model, int ComputerID)
        {
            // Find the requested server
            var comp = _context.Computer.Find(ComputerID);
            if (comp == null )
            {
                return NotFound();
            }
            else
            {
                var icon = $"/image/laptop/{ComputerID}";

                // Employee the computer is assigned to.
                var employeeAssigned = _context.Employee.Where(x => x.EmployeeId == comp.EmployeeId).FirstOrDefault();

                // int to hold the department Id for click-ability. -1 is the default if hardware is not assigned. 
                int departmentID = -1;
                // string to hold department name. empty string if hardware is unassigned. 
                string departmentName = "";

                // if an employee is assigned to this hardware then find their department
                if (employeeAssigned != null)
                {
                    var dep = _context.Department.Where(x => x.DepartmentId == employeeAssigned.DepartmentID).FirstOrDefault();
                    departmentID = dep.DepartmentId;
                    departmentName = dep.DepartmentName;
                }

                // Computer History
                List<object> ComputerHistory = new List<object>();

                // Formatting the data returned of this piece of hardware's history and adding it to a list.
                foreach (var entry in _context.HardwareHistory.Where(x => x.HardwareType.ToLower() == model && x.HardwareId == ComputerID))
                {
                    var empFirst = _context.Employee.Where(x => x.EmployeeId == entry.EmployeeId).Select(x => x.FirstName).FirstOrDefault();
                    var empLast = _context.Employee.Where(x => x.EmployeeId == entry.EmployeeId).Select(x => x.LastName).FirstOrDefault();
                    var employeeName = empFirst + " " + empLast;

                    var singleEntry = new { employeeName, entry.EventType, entry.EventDate, historyId = entry.HardwareHistoryId };
                    ComputerHistory.Add(singleEntry);
                }
                var computerClicked = nameof(Computer) + "/" + comp.ComputerId;

                var computerDetailPage = (new
                {
                    comp.IsDeleted,
                    computer = comp,
                    departmentName,
                    departmentID,
                    icon,
                    computerClicked,
                    employeeAssignedName = employeeAssigned != null ? employeeAssigned.FirstName + " " + employeeAssigned.LastName : "",
                    employeeAssignedId = employeeAssigned != null ? employeeAssigned.EmployeeId : -1,
                    ComputerHistory,
                    listOfEmployees = ListOfEmployees()
                });
                List<object> list = new List<object>();
                list.Add(computerDetailPage);
                return Ok(list);
            }
        }

        /* GET: api/detail/monitor/{id}
         * Function returns the monitor detail information.
         * Return: 
          {
                "monitor": {
                    "monitorId": int,
                    "make": string,
                    "model": string,
                    "resolution": int,
                    "inputs": string,
                    "employeeId": int,
                    "isAssigned": bool,
                    "textField": string,
                    "purchaseDate": date (as string),
                    "flatCost": decimal,
                    "costPerYear": decimal,
                    "isDeleted": bool,
                    "screenSize": int,
                    "mfg": string,
                    "renewalDate": date (as string),
                    "location": string,
                    "serialNumber": string,
                },
                "isDeleted": boolean,
                "departmentName : string,
                "departmentID : int,
                "icon": partial URL (as string),
                "monitorClicked" : string,
                "employeeAssignedName": string,
                "employeeAssignedId": number,
                "monitorHistory": [
                    {
                        "hardwareHistoryId": int,
                        "currentOwnerId": int,
                        "currentOwnerStartDate": date (as string),
                        "previousOwnerId": int,
                        "hardwareType": string,
                        "hardwareId": int,
                        "eventName": string,
                        "eventDescription": string,
                    },
                ]
            }
         */
        private IActionResult GetMonitorDetail(string model, int monitorID)
        {
            // Find the requested monitor
            var mn = _context.Monitor.Find(monitorID);
            if (mn == null )
            {
                return NotFound();
            }
            else
            {
                // Partial image string
                var icon = $"/image/monitor/{monitorID}";

                // Employee the monitor is assigned to.
                var employeeAssigned = _context.Employee.Where(x => x.EmployeeId == mn.EmployeeId).FirstOrDefault();

                // int to hold the department Id for click-ability. -1 is the default if hardware is not assigned. 
                int departmentID = -1;
                // string to hold department name. empty string if hardware is unassigned. 
                string departmentName = "";

                // if an employee is assigned to this hardware then find their department
                if (employeeAssigned != null)
                {
                    var dep = _context.Department.Where(x => x.DepartmentId == employeeAssigned.DepartmentID).FirstOrDefault();
                    departmentID = dep.DepartmentId;
                    departmentName = dep.DepartmentName;
                }


                // Monitor History
                List<object> MonitorHistory = new List<object>();

                // Formatting the data returned of this piece of hardware's history and adding it to a list.
                foreach (var entry in _context.HardwareHistory.Where(x => x.HardwareType.ToLower() == model && x.HardwareId == monitorID))
                {
                    var empFirst = _context.Employee.Where(x => x.EmployeeId == entry.EmployeeId).Select(x => x.FirstName).FirstOrDefault();
                    var empLast = _context.Employee.Where(x => x.EmployeeId == entry.EmployeeId).Select(x => x.LastName).FirstOrDefault();
                    var employeeName = empFirst + " " + empLast;

                    var singleEntry = new { employeeName, entry.EventType, entry.EventDate, historyId = entry.HardwareHistoryId };
                    MonitorHistory.Add(singleEntry);
                }

                var monitorClicked = nameof(Monitor) + "/" + mn.MonitorId;

                var monitorDetailPage = (new
                {
                    mn.IsDeleted,
                    monitor = mn,
                    departmentName,
                    departmentID,
                    icon,
                    monitorClicked,
                    employeeAssignedName = employeeAssigned != null ? employeeAssigned.FirstName + " " + employeeAssigned.LastName : "",
                    employeeAssignedId = employeeAssigned != null ? employeeAssigned.EmployeeId : -1,
                    MonitorHistory,
                    listOfEmployees = ListOfEmployees()
                });
                return Ok(new List<object> { monitorDetailPage });
            }
        }

        /* GET: api/detail/peripheral/{id}
         * Function returns the peripheral detail information.
         * Return: 
          {
                "peripheral": {
                    "peripheralId": int,
                    "peripheralName": string,
                    "peripheralType": string,
                    "textField": string,
                    "employeeId": int,
                    "isAssigned": bool,
                    "flatCost": decimal,
                    "purchaseDate": date (as string),
                    "costPerYear": decimal,
                    "isDeleted": bool,
                    "mfg": string,
                    "location": string,
                    "renewalDate": date (as string),
                    "serialNumber": string,
                },
                "isDeleted": boolean,
                "departmentName : string,
                "departmentID : int,
                "icon": partial URL (as string),
                "peripheralClicked" : string,
                "employeeAssignedName": string,
                "employeeAssignedId": number,
                "monitorHistory": [
                    {
                        "hardwareHistoryId": int,
                        "currentOwnerId": int,
                        "currentOwnerStartDate": date (as string),
                        "previousOwnerId": int,
                        "hardwareType": string,
                        "hardwareId": int,
                        "eventName": string,
                        "eventDescription": string,
                    },
                ]
            }
         */
        // TODO: Make the hardware getter generic.
        private IActionResult GetPeripheralDetail(string model, int peripheralID)
        {
            // Find the requested peripheral
            var pr = _context.Peripheral.Find(peripheralID);
            if (pr == null )
            {
                return NotFound();
            }
            else
            {
                // Partial image string
                var icon = $"/image/peripheral/{peripheralID}";

                // Employee the peripheral is assigned to.
                var employeeAssigned = _context.Employee.Where(x => x.EmployeeId == pr.EmployeeId).FirstOrDefault();

                // int to hold the department Id for click-ability. -1 is the default if hardware is not assigned. 
                int departmentID = -1;
                // string to hold department name. empty string if hardware is unassigned. 
                string departmentName = "";

                // if an employee is assigned to this hardware then find their department
                if (employeeAssigned != null)
                {
                    var dep = _context.Department.Where(x => x.DepartmentId == employeeAssigned.DepartmentID).FirstOrDefault();
                    departmentID = dep.DepartmentId;
                    departmentName = dep.DepartmentName;
                }

                // Peripheral History

                List<object> peripheralHistory = new List<object>();

                // Formatting the data returned of this piece of hardware's history and adding it to a list.
                foreach (var entry in _context.HardwareHistory.Where(x => x.HardwareType.ToLower() == model && x.HardwareId == peripheralID))
                {
                    var empFirst = _context.Employee.Where(x => x.EmployeeId == entry.EmployeeId).Select(x => x.FirstName).FirstOrDefault();
                    var empLast = _context.Employee.Where(x => x.EmployeeId == entry.EmployeeId).Select(x => x.LastName).FirstOrDefault();
                    var employeeName = empFirst + " " + empLast;

                    var singleEntry = new { employeeName, entry.EventType, entry.EventDate, historyId = entry.HardwareHistoryId };
                    peripheralHistory.Add(singleEntry);
                }

                var peripheralClicked = nameof(Peripheral) + "/" + pr.PeripheralId;
                var peripheralDetailPage = (new
                {
                    pr.IsDeleted,
                    peripheral = pr,
                    departmentName,
                    departmentID,
                    icon,
                    peripheralClicked,
                    employeeAssignedName = employeeAssigned != null ? employeeAssigned.FirstName + " " + employeeAssigned.LastName : "",
                    employeeAssignedId = employeeAssigned != null ? employeeAssigned.EmployeeId : -1,
                    peripheralHistory,
                    listOfEmployees = ListOfEmployees()
                });
                return Ok(new List<object> { peripheralDetailPage });
            }
        }
        private decimal? CalculatedHardwareCost<T>(List<int> employeeIds)
            where T : class, IHardwareBase
        {
            decimal? CostOfActHardwareInDep = 0.0m;
            // Get the table of the entity's type.
            DbSet<T> table = _context.Set<T>();
            table
            .Where(x => x.IsDeleted == false)
            .Where(x => x.FlatCost != null)
            .Where(x => x.EmployeeId != null)
            .ToList()
            .ForEach(hardware =>
                {
                    if (employeeIds.Contains(hardware.EmployeeId.Value))
                    {
                        CostOfActHardwareInDep += hardware.FlatCost;
                    }
                }
                );
            return CostOfActHardwareInDep;
        }

        private decimal? CalculatedHardwareCostForEmp<T>(int employeeId)
            where T : class, IHardwareBase
        {
            decimal? CostOfActHardwareInDep = 0.0m;
            // Get the table of the entity's type.
            DbSet<T> table = _context.Set<T>();
            CostOfActHardwareInDep = table
                .Where(x => x.EmployeeId == employeeId)
                .Where(x=> x.FlatCost != null)
                .Where(x=> x.IsDeleted != true)
                .Sum(x => x.FlatCost);
            return CostOfActHardwareInDep;
        }
    }
}
