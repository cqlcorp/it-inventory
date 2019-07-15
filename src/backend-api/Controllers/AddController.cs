using System;
using System.Collections.Generic;
using System.Linq;
using backend_api.Models;
using backend_api.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.DirectoryServices.AccountManagement;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using Microsoft.EntityFrameworkCore;

namespace backend_api.Controllers
{
    // [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class AddController : ContextController
    {

        public AddController(ITInventoryDBContext context) : base(context) { }


        /* GET: api/add/employeePrep
         * Returns [ {
         *              "myDomainUsers" : [
         *                  EmployeeName : string(with space between first and last name)
         *                  ], for all employees that are not currently in the database
         *              "departments" : [
         *                  {
         *                      "departmentName" : string,
         *                      "departmentID" : 8,
         *                      "icon": partial URL (as string),
         *                      "defaultHardware" [
         *                          ] all default hardware of each department,
         *                      "defaultLicenses" [
         *                          ] all default licenses of each department,
         *                      "defaultSoftware" [
         *                          ] all default software of each department,
         *                     ] for all the departments that are not deleted
         *               "unassigned hardware" : [
         *                  {
         *                      "Id" : int,
         *                      "type" : string,
         *                      "name" : string,
         *                      ] for all the unassigned hardware
         *                  }
         *                  "unassigned software" : [
         *                  {
         *                      "Id" : int,
         *                      "type" : string,
         *                      "name" : string,
         *                      ] for all the unassigned software
         *                  }
         *                  "unassigned licenses" : [
         *                  {
         *                      "Id" : int,
         *                      "type" : string,
         *                      "name" : string,
         *                      } for all the unassigned licenses
         *                  ]
         *            }
         *    ]        
         */

        [HttpGet]
        [Route("EmployeePrep")]
        public IActionResult GetEmployeePrep()
        {
            // list to hold CQL distribution employees that are not currently added to the application
            var myDomainUsers = new List<string>();

            // Finding our Active Directory.
            using (var ctx = new PrincipalContext(ContextType.Domain, "CQLCORP"))
            {
                var userPrinciple = new UserPrincipal(ctx);
                // Finding the CQL distribution group within AD
                GroupPrincipal gp = GroupPrincipal.FindByIdentity(ctx, "CQL Distribution");

                var ADIds = _context.Employee.Select(x => x.Adguid).ToList();
                // loop to check all employees if they exist and have not yet been added to our database.
                // if they are not added then add their name to our list of people possible to add.
                using (var search = new PrincipalSearcher(userPrinciple))
                {
                    foreach (var domainUser in search.FindAll())
                    {
                        if (domainUser.DisplayName != null && domainUser.Guid != null && domainUser.IsMemberOf(gp) && !ADIds.Contains(domainUser.Guid.Value))
                        {
                            myDomainUsers.Add(domainUser.DisplayName);
                        }
                    }
                }

            }

            List<object> departments = new List<object>();
            foreach (var dep in _context.Department.Where(x => x.IsDeleted == false))
            {
                // if the department is any department apart from utilities does not have default programs
                if (dep.DepartmentName != "Utilities")
                {
                    // pull stringifyed default hardware and software out into a nice JSON object :) using JSON package.
                    JObject defaultHardware = JObject.Parse(dep.DefaultHardware);
                    JObject defaultPrograms = JObject.Parse(dep.DefaultPrograms);

                    // image for the department
                    string icon = $"/image/department/{dep.DepartmentId}";

                    // the necessary returnables
                    departments.Add(new
                    {
                        dep.DepartmentName,
                        dep.DepartmentId,
                        icon,
                        DefaultHardware = defaultHardware["DefaultHardware"],
                        DefaultLicenses = defaultPrograms["license"],
                        DefaultSoftware = defaultPrograms["software"],
                    });
                }

                // if the department is utilities then it does not have default programs
                else
                {
                    string icon = $"/image/department/{dep.DepartmentId}";

                    departments.Add(new
                    {
                        dep.DepartmentName,
                        dep.DepartmentId,
                        icon
                    });

                }
            }

            // list that will hold the unassigned hardware
            List<object> UnassignedHardware = new List<object>();

            // loop with lambda that finds the unassigned, not deleted monitors and adds the necessary returnables of them into a list 
            foreach (var mon in _context.Monitor.Where(x => x.EmployeeId == null && x.IsDeleted == false))
            {
                var monitorName = mon.Make + " " + mon.Model;
                var monitor = new
                {
                    mon.MonitorId,
                    type = nameof(Monitor),
                    monitorName
                };
                UnassignedHardware.Add(monitor);
            }
            // loop with lambda that finds the unassigned, not deleted servers and adds the necessary returnables of them into a list 
            foreach (var serv in _context.Server.Where(x => x.EmployeeId == null && x.IsDeleted == false))
            {
                var serverName = serv.Make + " " + serv.Model;
                var server = new
                {
                    serv.ServerId,
                    type = nameof(Server),
                    serverName
                };
                UnassignedHardware.Add(server);
            }

            // loop with lambda that finds the unassigned, not deleted computers and adds the necessary returnables of them into a list 
            foreach (var comp in _context.Computer.Where(x => x.EmployeeId == null && x.IsDeleted == false))
            {
                var compName = comp.Make + " " + comp.Model;
                var computer = new
                {
                    comp.ComputerId,
                    type = nameof(Computer),
                    compName
                };
                UnassignedHardware.Add(computer);
            }

            // loop with lambda that finds the unassigned, not deleted peripherals and adds the necessary returnables of them into a list 
            foreach (var periph in _context.Peripheral.Where(x => x.EmployeeId == null && x.IsDeleted == false))
            {
                var periphName = periph.PeripheralName + " " + periph.PeripheralType;
                var peripheral = new
                {
                    periph.PeripheralId,
                    type = nameof(Peripheral),
                    periphName
                };
                UnassignedHardware.Add(peripheral);
            }

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

            var empPrep = (new
            {
                myDomainUsers,
                departments,
                UnassignedHardware,
                UnassignedSoftware,
                UnassignedLicenses
            });
            return Ok(new List<object> { empPrep });
        }

        /* POST: api/add/employee
         * Takes in as input:
         * {
         *           "Employee": {
         *           "FirstName": String,
         *           "LastName": String,
         *           "HireDate": String,
         *           "Role": String,
         *           "DepartmentID": int
         *       },
         *       "HardwareAssigned": [
         *           {
         *               "Type": String,
         *               "ID": int
         *           }
         *       ],
         *       "ProgramAssigned": [
         *           {
         *               "ID": int
         *           },
         *           {	
         *	            "ID": int
         *           }
         *       ]
         *   }
         * 
         */

        [HttpPost]
        [Route("Employee")]
        public IActionResult PostEmployee([FromBody] PostEmployeeInputModel input)
        {
            // concatenating first and last name for comparison reasons
            var userName = input.Employee.FirstName + "." + input.Employee.LastName;

            // find our active directory context so we can find the guid of the employee we are adding.
            using (var adContext = new PrincipalContext(ContextType.Domain, "CQLCORP"))
            {
                var user = UserPrincipal.FindByIdentity(adContext, userName);
                // creating employee object to added to the database and then saved.
                var emp = new Employee()
                {
                    HireDate = input.Employee.HireDate,
                    DepartmentID = input.Employee.DepartmentID,
                    IsDeleted = false,
                    UserSettings = "",
                    FirstName = input.Employee.FirstName,
                    LastName = input.Employee.LastName,
                    Email = "",
                    Role = input.Employee.Role,
                    Adguid = user.Guid.Value
                };
                _context.Employee.Add(emp);

                var AuthEmp = new AuthIdserver()
                {
                    ActiveDirectoryId = user.Guid.Value,
                    RefreshToken = "",
                    IsAdmin = input.Employee.IsAdmin
                };
                _context.AuthIdserver.Add(AuthEmp);
                _context.SaveChanges();


                // if there is any hardware that is to be assigned from the front end
                if (input.HardwareAssigned != null)
                {
                    // loop through hardware and depending on what type the hardware is, then add the hardware to the specific table. 
                    foreach (var hardware in input.HardwareAssigned)
                    {
                        switch (hardware.Type.ToLower())
                        {
                            case "monitor":
                                var mon = _context.Monitor.Find(hardware.ID);
                                mon.EmployeeId = emp.EmployeeId;
                                mon.IsAssigned = true;
                                UpdateHardwareHistory(true, emp.EmployeeId, hardware.ID, hardware.Type);
                                _context.SaveChanges();
                                break;
                            case "peripheral":
                                var periph = _context.Peripheral.Find(hardware.ID);
                                periph.EmployeeId = emp.EmployeeId;
                                periph.IsAssigned = true;
                                UpdateHardwareHistory(true, emp.EmployeeId, hardware.ID, hardware.Type);
                                _context.SaveChanges();
                                break;
                            case "computer":
                                var comp = _context.Computer.Find(hardware.ID);
                                comp.EmployeeId = emp.EmployeeId;
                                comp.IsAssigned = true;
                                UpdateHardwareHistory(true, emp.EmployeeId, hardware.ID, hardware.Type);
                                _context.SaveChanges();
                                break;
                            case "server":
                                var server = _context.Server.Find(hardware.ID);
                                server.EmployeeId = emp.EmployeeId;
                                server.IsAssigned = true;
                                UpdateHardwareHistory(true, emp.EmployeeId, hardware.ID, hardware.Type);
                                _context.SaveChanges();
                                break;

                        }

                    }
                }
                // list to hold the histories of programs that will be added
                List<ProgramHistory> programHistories = new List<ProgramHistory>();

                // if there are any programs to be assigned from the front-end
                if (input.ProgramAssigned != null)
                {
                    foreach (var program in input.ProgramAssigned)
                    {
                        var prog = _context.Program.Find(program.ID);
                        prog.EmployeeId = emp.EmployeeId;
                        programHistories.Add(UpdateProgramHistory(true, emp.EmployeeId, program.ID));
                    }
                    // Save multiple entries at once
                    _context.ProgramHistory.AddRange(programHistories);
                    _context.SaveChanges();

                }
                // if we get here then the various fields were created and changed and now we can return 201 created.
                return StatusCode(201);
            }
        }

        /* GET: api/add/departmentprep
         * Returns the list of hardware, licenses, and software to be used
         *      during the creation of a new department.
         * Returns: [ {
                        "hardware": string[],
                        "licenses": string[],
                        "software": string[],
                    } ]
         * 
         */
        [HttpGet]
        [Route("DepartmentPrep")]
        public IActionResult GetDepartmentPrep()
        {
            // List of generic types that can be assigned as defaults.
            List<string> hardware = new List<string> { "Server", "Laptop", "Monitor" };

            // Get the type of peripherals. Types are generic but more specific than "Peripheral"
            List<string> peripherals = _context.Peripheral.Where(pr => !pr.IsDeleted).GroupBy(pr => pr.PeripheralType).Select(pr => pr.FirstOrDefault()).Select(pr => pr.PeripheralType).ToList();
            hardware.AddRange(peripherals);

            // Get the names of licenses and software that are not deleted
            IQueryable<string> licenses = _context.Program.Where(prog => prog.IsLicense && !prog.IsDeleted).GroupBy(prog => prog.ProgramName).Select(prog => prog.FirstOrDefault()).Select(prog => prog.ProgramName);
            IQueryable<string> software = _context.Program.Where(prog => !prog.IsLicense && !prog.IsDeleted).GroupBy(prog => prog.ProgramName).Select(prog => prog.FirstOrDefault()).Select(prog => prog.ProgramName);

            // Return JSON in a list :)
            return Ok(new[] { new
                {
                    hardware,
                    licenses,
                    software,
                }
            });
        }

        /* POST: api/add/department
         * Will add a row to the department table
         * Param input format:
                {
                    "DefaultHardware": {
                        "DefaultHardware": string[],
                    },
                    "DefaultPrograms": {
                        "license": string[],
                        "software": string[],
                    },
                    "Name": string,
                }
         * Return: 201 if created. Else, 400 bad request. 
         */
        [HttpPost]
        [Route("Department")]
        public IActionResult PostDepartment([FromBody] DepartmentInput input)
        {
            // Try to add a department entity.
            try
            {
                Department dep = new Department()
                {
                    // Convert the objects to strings to store in the db.
                    DefaultHardware = JsonConvert.SerializeObject(input.DefaultHardware),
                    DefaultPrograms = JsonConvert.SerializeObject(input.DefaultPrograms),
                    DepartmentName = input.Name,
                    IsDeleted = false,
                };
                _context.Department.Add(dep);
                _context.SaveChanges();

                // if we get here then the various fields were created and changed and now we can return 201 created.
                return StatusCode(201);
            }
            catch (Exception e)
            {
                return BadRequest(error: e.Message);
            }
        }

        /* POST: api/add/Program
         * Takes in as input:
         * {   "Program" : {
         *          "numberOfPrograms" : int,
         *          "ProgramName" : string,
         *          "ProgramCostPerYear" : Decimal,
         *          "ProgramFlatCost" : Decimal,
         *          "ProgramLicenseKey" : string,
         *          "IsLicense" : bool,
         *          "ProgramDescription" : string,
         *          "ProgramPurchaseLink" : string,
         *          "DateBought" : DateTime,
         *          "RenewalDate" : DateTime,
         *          "MonthsPerRenewal" : int
         *     }
         * }
         */
        [HttpPost]
        [Route("Program")]
        public IActionResult PostProgram([FromBody] PostProgramInputModel input)
        {
            // list to hold the congruent programs that will be added.
            List<Models.Program> Programs = new List<Models.Program>();

            // checking to see if number of programs is not less than 1
            if (input.Program.NumberOfPrograms < 1)
            {
                return BadRequest("number of programs must be greater than 0");
            }

            // list to hold the congruent histories of programs that will be added
            List<ProgramHistory> programHistories = new List<ProgramHistory>();
            try
            {
                // adding the correct number of programs that was specified 
                for (int i = 0; i < input.Program.NumberOfPrograms; i++)
                {
                    var Prog = new Models.Program()
                    {
                        ProgramName = input.Program.ProgramName,
                        ProgramCostPerYear = input.Program.ProgramCostPerYear,
                        ProgramFlatCost = input.Program.ProgramFlatCost,
                        ProgramLicenseKey = input.Program.ProgramLicenseKey,
                        IsLicense = input.Program.IsLicense,
                        EmployeeId = null,
                        Description = input.Program.ProgramDescription,
                        ProgramPurchaseLink = input.Program.ProgramPurchaseLink,
                        HasPlugIn = false,
                        IsDeleted = false,
                        IsCostPerYear = input.Program.MonthsPerRenewal != null && input.Program.MonthsPerRenewal - 12 >= 0 ? true : false,
                        DateBought = input.Program.DateBought,
                        RenewalDate = input.Program.RenewalDate,
                        MonthsPerRenewal = input.Program.MonthsPerRenewal

                    };

                    // add the individual program to our list to hold the congruent program
                    Programs.Add(Prog);

                }
                // Save multiple entities at once.
                _context.Program.AddRange(Programs);
                _context.SaveChanges();

                // now that the programs have been added to the database, now we can generate the program history entries
                // for the programs we just added
                foreach (var prog in _context.Program.Where(x => x.ProgramName == input.Program.ProgramName))
                {
                    var History = (new ProgramHistory
                    {
                        EmployeeId = null,
                        ProgramId = prog.ProgramId,
                        EventType = "Bought",
                        EventDate = prog.DateBought.Value

                    });
                    programHistories.Add(History);
                }
                // Save multiple entries at once
                _context.ProgramHistory.AddRange(programHistories);
                _context.SaveChanges();


                // if we get here then the various fields were created and changed and now we can return 201 created.
                return StatusCode(201);
            }
            catch
            {
                return BadRequest();
            }
        }
        /* POST: api/add/Plugin
         * Takes in as input:
         * {
	     *     "ProgramName" : String,
	     *     "PluginName" : int,
	     *     "PluginFlatCost" : Decimal,
	     *     "TextField" : string,
	     *     "PluginCostPerYear" : Decimal,
	     *     "RenewalDate" : DateTime,
	     *     "MonthsPerRenewal" : int,
         *     "DateBought" : DateTime
         * }
         */

        [HttpPost]
        [Route("Plugin")]
        public IActionResult PostPlugin([FromBody] PostPluginInputModel input)
        {
            if (!(_context.Program.Select(x => x.ProgramName).ToList().Contains(input.ProgramName)))
                return BadRequest("No such program exists");
            var plugin = new Plugins()
            {
                PluginName = input.PluginName,
                PluginFlatCost = input.PluginFlatCost,
                ProgramId = _context.Program.Where(x => x.ProgramName == input.ProgramName).Select(x => x.ProgramId).First(),
                TextField = input.TextField,
                PluginCostPerYear = input.PLuginCostPerYear,
                IsDeleted = false,
                ProgramName = input.ProgramName,
                RenewalDate = input.RenewalDate,
                MonthsPerRenewal = input.MonthsPerRenewal,
                Datebought = input.DateBought,
                IsCostPerYear = input.MonthsPerRenewal != null && input.MonthsPerRenewal - 12 >= 0 ? true : false,
            };
            _context.Add(plugin);
            _context.SaveChanges();

            // find the specific program tied to this plugin that was just updated
            var programTiedToPlugin = _context.Program.Find(plugin.ProgramId);

            _context.Program.Where(x => x.ProgramName == programTiedToPlugin.ProgramName).ToList().ForEach(x => x.HasPlugIn = true);
            _context.SaveChanges();
            return StatusCode(201);

        }



        /* GET: api/add/hardwarePrep
         * Returns: [ {
         *              employeeName: string,
         *              employeeId: int,
         *             } ,.. ]
         * Will return an array of objects with the employee name and
         *   id for every non-archived employee to be used when assigning 
         *   a piece of hardware.
         */
        [HttpGet]
        [Route("HardwarePrep")]
        public IActionResult GetHardwarePrep()
        {
            return Ok(ListOfEmployees());
        }

        /* POST: api/add/monitor
         * Method will add a monitor row to the monitor table with the specified
         *   attribute values if they are valid and will add the appropriate hardware history.
         * Input format:
                {
	                "Entity" : {
		                "Make" : string?,
		                "Model" : string?,
		                "Resolution" : integer?,
		                "Inputs" : string?,
		                "EmployeeId" : int?,
		                "TextField" : string?,
		                "PurchaseDate" : string? (formatted yyyy-mm-dd),
		                "FlatCost" : decimal?,
		                "CostPerYear" : decimal?,
		                "ScreenSize" : float?,
		                "Mfg" : string?,
		                "RenewalDate" : string? (formatted yyyy-mm-dd),
		                "Location" : "xx"? (either GR or AA),
		                "SerialNumber" : string?,
		                "MonthsPerRenewal" : int?,
	                }
                }
         * Return: 201 created if successful, and 400 bad request if not.
         */
        [HttpPost]
        [Route("Monitor")]
        public IActionResult PostMonitor([FromBody] EntityInput<Monitor> input)
        {
            return PostHardware(input.Entity, _context.Monitor);
        }

        /* POST: api/add/server
         * Method will add a server row to the server table with the specified
         *   attribute values if they are valid and will add the appropriate hardware history.
         * Input format:
                {
	                "Entity" : 
	                {
		                "Fqdn" : string?,
		                "NumberOfCores" : int?,
		                "OperatingSystem" : string?,
		                "Ram" : int?,
		                "Virtualize" : bool,
		                "RenewalDate" : string? (formatted yyyy-mm-dd),
		                "EmployeeId" : int?,
		                "PurchaseDate" : string? (formatted yyyy-mm-dd),
		                "FlatCost" : decimal?,
		                "EndOfLife" : string? (formatted yyyy-mm-dd),
		                "TextField" : string?,
		                "CostPerYear" : decimal?,
		                "MFG" : string?,
		                "Make" : string?,
		                "Model" : string?,
		                "IPAddress" : string?,
		                "SAN" : string?,
		                "LocalHHD" : string?,
		                "Location" : "xx"? (either GR or AA),
		                "SerialNumber" : string?,
		                "MonthsPerRenewal" : int?,
	                }
                }
         * Return: 201 created if successful, and 400 bad request if not.
         */
        [HttpPost]
        [Route("Server")]
        public IActionResult PostServer([FromBody] EntityInput<Server> input)
        {
            return PostHardware(input.Entity, _context.Server);
        }

        /* POST: api/add/laptop
         * Method will add a computer row to the computer table with the specified
         *   attribute values if they are valid and will add the appropriate hardware history.
         * Input format:
                {
	                "Entity" : {
		                "Cpu" : string?,
		                "Ramgb" : int?,
		                "Ssdgb" : int?,
		                "PurchaseDate" : string? (formatted yyyy-mm-dd),
		                "RenewalDate" : string? (formatted yyyy-mm-dd),
		                "FlatCost" : decimal?,
		                "MonitorOutput" : string?,
		                "EndOfLife" : string? (formatted yyyy-mm-dd),
		                "EmployeeId" : int?,
		                "TextField" : string?,
		                "ScreenSize" : float?,
		                "CostPerYear" : decimal?,
		                "Resolution" : decimal?,
		                "Mfg" : string?,
		                "Make" : string?,
		                "Model" : string?,
		                "Fqdn" : string?,
		                "Location" : "xx"? (either GR or AA),
		                "SerialNumber" : string?,
		                "MonthsPerRenewal" : int?
	                }
                }
         * Return: 201 created if successful, and 400 bad request if not.
         */
        [HttpPost]
        [Route("Laptop")]
        [Route("Computer")]
        public IActionResult PostComputer([FromBody] EntityInput<Computer> input)
        {
            return PostHardware(input.Entity, _context.Computer);
        }

        /* POST: api/add/peripheral
         * Method will add a peripheral row to the peripheral table with the specified
         *   attribute values if they are valid and will add the appropriate hardware history.
         * Input format:
                {
	                "Entity" : {
		                "PeripheralName" : string?,
		                "PeripheralType" : string?,
		                "TextField" : string?,
		                "EmployeeId" : int?,
		                "FlatCost" : decimal?,
		                "PurchaseDate" : string? (formatted yyyy-mm-dd),,
		                "CostPerYear" : decimal?,
		                "Mfg" : string?,
		                "Location" : "xx"? (either GR or AA),
		                "RenewalDate" : string? (formatted yyyy-mm-dd),
		                "SerialNumber" : string?,
		                "MonthsPerRenewal" : int?,
	                }
                }
         * Return: 201 created if successful, and 400 bad request if not.
         */
        [HttpPost]
        [Route("Peripheral")]
        public IActionResult PostPeripheral([FromBody] EntityInput<Peripheral> input)
        {
            return PostHardware(input.Entity, _context.Peripheral);
        }

        /* PostHardware<T>(hardware, table) is a method to post any hardware type 
         *   to it's corresponding table and add hardware history.
         * Return: 201 created if successful, and 400 bad request if not.
         */
        private IActionResult PostHardware<T>(T hardware, DbSet<T> table)
            where T : class, IHardwareBase
        {
            int? EmployeeId = hardware.EmployeeId;

            // Update values we don't want touched by the endpoint call.
            hardware.IsAssigned = EmployeeId != null ? true : false;
            hardware.IsDeleted = false;

            // NOTE: CostPerYear is calculated on the front end.

            // Get class name at runtime
            string type = GetClassName(hardware);

            try
            {
                table.Add(hardware);

                // Save the changes to db so given Id can be accessed.
                _context.SaveChanges();
            }
            catch
            {
                return BadRequest($"Problem saving new {type} entity to the database");
            }

            int id = hardware.GetId();

            try
            {
                // Add the history for date bought and for assigning an employee.
                // If the PurchaseDate is null, then use the current date and time.
                UpdateHardwareHistory(null, type, id, "Bought", hardware.PurchaseDate != null ? hardware.PurchaseDate : DateTime.Now);

                // Add history for assigning employee
                if (EmployeeId != null)
                {
                    UpdateHardwareHistory(EmployeeId, type, id, "Assigned", DateTime.Now);
                }

                _context.SaveChanges();
            }
            catch
            {
                return BadRequest($"New {type} entity was created but there was an issue creating history for the entity");
            }

            // If we make it here, everything must have succeeded
            return StatusCode(201);
        }

        /* UpdateHardwareHistory(empId, hardwareType, hardwareId, eventType, date) will add an entry into the 
         *   hardware history table.
         */
        // TODO: Abstract this
        private void UpdateHardwareHistory(int? empId, string hardwareType, int hardwareId, string eventType, DateTime? date)
        {
            _context.HardwareHistory.Add(new HardwareHistory
            {
                EmployeeId = empId,
                HardwareType = hardwareType,
                HardwareId = hardwareId,
                EventType = eventType,
                EventDate = date,
            });
        }
    }
}


