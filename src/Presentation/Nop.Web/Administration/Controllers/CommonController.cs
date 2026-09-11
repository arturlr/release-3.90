using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Security.Principal;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.StaticFiles;
using Nop.Admin.Extensions;
using Nop.Admin.Models.Common;
using Nop.Core;
using Nop.Core.Caching;
using Nop.Core.Domain.Catalog;
using Nop.Core.Domain.Directory;
using Nop.Core.Infrastructure;
using Nop.Core.Plugins;
using Nop.Services.Common;
using Nop.Services.Configuration;
using Nop.Services.Customers;
using Nop.Services.Directory;
using Nop.Services.Helpers;
using Nop.Services.Localization;
using Nop.Services.Orders;
using Nop.Services.Payments;
using Nop.Services.Security;
using Nop.Services.Seo;
using Nop.Services.Shipping;
using Nop.Services.Stores;
using Nop.Web.Framework.Controllers;
using Nop.Web.Framework.Kendoui;
using Nop.Web.Framework.Security;
using Nop.Web.Framework.Mvc;

namespace Nop.Admin.Controllers
{
    public partial class CommonController : BaseAdminController
    {
        #region Fields

        private readonly IPaymentService _paymentService;
        private readonly IShippingService _shippingService;
        private readonly IShoppingCartService _shoppingCartService;
        private readonly ICurrencyService _currencyService;
        private readonly IMeasureService _measureService;
        private readonly ICustomerService _customerService;
        private readonly IUrlRecordService _urlRecordService;
        private readonly IWebHelper _webHelper;
        private readonly CurrencySettings _currencySettings;
        private readonly MeasureSettings _measureSettings;
        private readonly IDateTimeHelper _dateTimeHelper;
        private readonly ILanguageService _languageService;
        private readonly IWorkContext _workContext;
        private readonly IStoreContext _storeContext;
        private readonly IPermissionService _permissionService;
        private readonly ILocalizationService _localizationService;
        private readonly ISearchTermService _searchTermService;
        private readonly ISettingService _settingService;
        private readonly IStoreService _storeService;
        private readonly CatalogSettings _catalogSettings;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IMaintenanceService _maintenanceService;

        #endregion

        #region Constructors

        public CommonController(IPaymentService paymentService,
            IShippingService shippingService,
            IShoppingCartService shoppingCartService,
            ICurrencyService currencyService,
            IMeasureService measureService,
            ICustomerService customerService,
            IUrlRecordService urlRecordService,
            IWebHelper webHelper,
            CurrencySettings currencySettings,
            MeasureSettings measureSettings,
            IDateTimeHelper dateTimeHelper,
            ILanguageService languageService,
            IWorkContext workContext,
            IStoreContext storeContext,
            IPermissionService permissionService,
            ILocalizationService localizationService,
            ISearchTermService searchTermService,
            ISettingService settingService,
            IStoreService storeService,
            CatalogSettings catalogSettings,
            IHttpContextAccessor httpContextAccessor,
            IMaintenanceService maintenanceService)
        {
            this._paymentService = paymentService;
            this._shippingService = shippingService;
            this._shoppingCartService = shoppingCartService;
            this._currencyService = currencyService;
            this._measureService = measureService;
            this._customerService = customerService;
            this._urlRecordService = urlRecordService;
            this._webHelper = webHelper;
            this._currencySettings = currencySettings;
            this._measureSettings = measureSettings;
            this._dateTimeHelper = dateTimeHelper;
            this._languageService = languageService;
            this._workContext = workContext;
            this._storeContext = storeContext;
            this._permissionService = permissionService;
            this._localizationService = localizationService;
            this._searchTermService = searchTermService;
            this._settingService = settingService;
            this._storeService = storeService;
            this._catalogSettings = catalogSettings;
            this._httpContextAccessor = httpContextAccessor;
            this._maintenanceService = maintenanceService;
        }

        #endregion

        #region Utitlies

        private bool IsDebugAssembly(Assembly assembly)
        {
            var attribs = assembly.GetCustomAttributes(typeof(System.Diagnostics.DebuggableAttribute), false);

            if (attribs.Length > 0)
            {
                var attr = attribs[0] as System.Diagnostics.DebuggableAttribute;
                if (attr != null)
                {
                    return attr.IsJITOptimizerDisabled;
                }
            }

            return false;
        }

        private DateTime GetBuildDate(Assembly assembly, TimeZoneInfo target = null)
        {
            var filePath = assembly.Location;

            const int cPeHeaderOffset = 60;
            const int cLinkerTimestampOffset = 8;

            var buffer = new byte[2048];

            using (var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
            {
                //TASK 8.3 - a single Stream.Read is not guaranteed to fill the buffer, so the PE
                //header offsets read below could be computed from a partially-filled buffer.
                //ReadAtLeast loops; throwOnEndOfStream:false preserves 3.90's behaviour for an
                //assembly file shorter than 2048 bytes (buffer stays zero-filled from that point).
                stream.ReadAtLeast(buffer, 2048, throwOnEndOfStream: false);
            }

            var offset = BitConverter.ToInt32(buffer, cPeHeaderOffset);
            var secondsSince1970 = BitConverter.ToInt32(buffer, offset + cLinkerTimestampOffset);
            var epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

            var linkTimeUtc = epoch.AddSeconds(secondsSince1970);

            var tz = target ?? TimeZoneInfo.Local;
            var localTime = TimeZoneInfo.ConvertTimeFromUtc(linkTimeUtc, tz);

            return localTime;
        }

        #endregion

        #region Methods

        public virtual ActionResult SystemInfo()
        {
            if (!_permissionService.Authorize(StandardPermissionProvider.ManageMaintenance))
                return AccessDeniedView();

            var model = new SystemInfoModel();
            model.NopVersion = NopVersion.CurrentVersion;
            try
            {
                model.OperatingSystem = Environment.OSVersion.VersionString;
            }
            catch (Exception) { }
            try
            {
                model.AspNetInfo = RuntimeEnvironment.GetSystemVersion();
            }
            catch (Exception) { }
            //TASK 8.3 - AppDomain.CurrentDomain.IsFullyTrusted always returns true on .NET (there
            //is no Code Access Security), so this try/catch could only ever produce "True". The
            //value is now set below, alongside the removal of CommonHelper.GetTrustLevel(), so the
            //page has one authoritative answer instead of two.
            model.ServerTimeZone = TimeZone.CurrentTimeZone.StandardName;
            model.ServerLocalTime = DateTime.Now;
            model.UtcTime = DateTime.UtcNow;
            model.CurrentUserTime = _dateTimeHelper.ConvertToUserTime(DateTime.Now);
            model.HttpHost = _webHelper.ServerVariables("HTTP_HOST");

            //TASK 8.3 - HttpRequest.ServerVariables has no ASP.NET Core counterpart and is not
            //recoverable: System.Web's server-variable collection was populated by IIS/ISAPI and
            //held both request headers AND non-header values (SERVER_SOFTWARE, LOCAL_ADDR,
            //APPL_PHYSICAL_PATH, ...). ASP.NET Core exposes only what the protocol actually
            //carried. Task 2.4 already recorded the same loss for IWebHelper.ServerVariables
            //(runtime-deferrals.md section 2): HTTP_* names map back to the originating header and
            //everything else returns "".
            //
            //This page therefore now lists the REQUEST HEADERS, which is the honest subset. The
            //3.90 "ALL_" filter is preserved (ALL_HTTP / ALL_RAW were synthesised aggregate
            //variables and are simply never present now), and each header's values are joined the
            //same way StringValues renders them, so a multi-valued header reads as it did.
            var request = _httpContextAccessor.HttpContext != null
                ? _httpContextAccessor.HttpContext.Request
                : null;
            if (request != null)
            {
                foreach (var header in request.Headers)
                {
                    if (header.Key.StartsWith("ALL_")) continue;

                    model.ServerVariables.Add(new SystemInfoModel.ServerVariableModel
                    {
                        Name = header.Key,
                        Value = header.Value.ToString()
                    });
                }
            }
            //Environment.GetEnvironmentVariable("USERNAME");

            //TASK 8.3 - CommonHelper.GetTrustLevel() and AspNetHostingPermissionLevel were DELETED
            //at task 2.4: there is no Code Access Security and no medium trust on .NET, so there is
            //no trust level to query and nothing can be partially trusted. Per runtime-deferrals.md
            //section 2 this page reports "Full" unconditionally, and the guard below - which existed
            //only because Assembly.Location threw under partial trust - collapses to the
            //!IsDynamic test, which is the part that is still real (a dynamic assembly has no
            //Location).
            model.IsFullTrust = "Full";

            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                var loadedAssembly = new SystemInfoModel.LoadedAssembly
                {
                    FullName = assembly.FullName,
                   
                };
                //ensure no exception is thrown
                try
                {
                    var canGetLocation = !assembly.IsDynamic;
                    loadedAssembly.Location = canGetLocation ? assembly.Location : null;
                    loadedAssembly.IsDebug = IsDebugAssembly(assembly);
                    loadedAssembly.BuildDate = canGetLocation ? (DateTime?)GetBuildDate(assembly, TimeZoneInfo.Local) : null;
                }
                catch (Exception) { }
                model.LoadedAssemblies.Add(loadedAssembly);
            }

            return View(model);
        }

        public virtual ActionResult Warnings()
        {
            if (!_permissionService.Authorize(StandardPermissionProvider.ManageMaintenance))
                return AccessDeniedView();

            var model = new List<SystemWarningModel>();

            //store URL
            var currentStoreUrl = _storeContext.CurrentStore.Url;
            if (!String.IsNullOrEmpty(currentStoreUrl) &&
                (currentStoreUrl.Equals(_webHelper.GetStoreLocation(false), StringComparison.InvariantCultureIgnoreCase)
                ||
                currentStoreUrl.Equals(_webHelper.GetStoreLocation(true), StringComparison.InvariantCultureIgnoreCase)
                ))
                model.Add(new SystemWarningModel
                {
                    Level = SystemWarningLevel.Pass,
                    Text = _localizationService.GetResource("Admin.System.Warnings.URL.Match")
                });
            else
                model.Add(new SystemWarningModel
                {
                    Level = SystemWarningLevel.Warning,
                    Text = string.Format(_localizationService.GetResource("Admin.System.Warnings.URL.NoMatch"), currentStoreUrl, _webHelper.GetStoreLocation(false))
                });


            //primary exchange rate currency
            var perCurrency = _currencyService.GetCurrencyById(_currencySettings.PrimaryExchangeRateCurrencyId);
            if (perCurrency != null)
            {
                model.Add(new SystemWarningModel
                {
                    Level = SystemWarningLevel.Pass,
                    Text = _localizationService.GetResource("Admin.System.Warnings.ExchangeCurrency.Set"),
                });
                if (perCurrency.Rate != 1)
                {
                    model.Add(new SystemWarningModel
                    {
                        Level = SystemWarningLevel.Fail,
                        Text = _localizationService.GetResource("Admin.System.Warnings.ExchangeCurrency.Rate1")
                    });
                }
            }
            else
            {
                model.Add(new SystemWarningModel
                {
                    Level = SystemWarningLevel.Fail,
                    Text = _localizationService.GetResource("Admin.System.Warnings.ExchangeCurrency.NotSet")
                });
            }

            //primary store currency
            var pscCurrency = _currencyService.GetCurrencyById(_currencySettings.PrimaryStoreCurrencyId);
            if (pscCurrency != null)
            {
                model.Add(new SystemWarningModel
                {
                    Level = SystemWarningLevel.Pass,
                    Text = _localizationService.GetResource("Admin.System.Warnings.PrimaryCurrency.Set"),
                });
            }
            else
            {
                model.Add(new SystemWarningModel
                {
                    Level = SystemWarningLevel.Fail,
                    Text = _localizationService.GetResource("Admin.System.Warnings.PrimaryCurrency.NotSet")
                });
            }


            //base measure weight
            var bWeight = _measureService.GetMeasureWeightById(_measureSettings.BaseWeightId);
            if (bWeight != null)
            {
                model.Add(new SystemWarningModel
                {
                    Level = SystemWarningLevel.Pass,
                    Text = _localizationService.GetResource("Admin.System.Warnings.DefaultWeight.Set"),
                });

                if (bWeight.Ratio != 1)
                {
                    model.Add(new SystemWarningModel
                    {
                        Level = SystemWarningLevel.Fail,
                        Text = _localizationService.GetResource("Admin.System.Warnings.DefaultWeight.Ratio1")
                    });
                }
            }
            else
            {
                model.Add(new SystemWarningModel
                {
                    Level = SystemWarningLevel.Fail,
                    Text = _localizationService.GetResource("Admin.System.Warnings.DefaultWeight.NotSet")
                });
            }


            //base dimension weight
            var bDimension = _measureService.GetMeasureDimensionById(_measureSettings.BaseDimensionId);
            if (bDimension != null)
            {
                model.Add(new SystemWarningModel
                {
                    Level = SystemWarningLevel.Pass,
                    Text = _localizationService.GetResource("Admin.System.Warnings.DefaultDimension.Set"),
                });

                if (bDimension.Ratio != 1)
                {
                    model.Add(new SystemWarningModel
                    {
                        Level = SystemWarningLevel.Fail,
                        Text = _localizationService.GetResource("Admin.System.Warnings.DefaultDimension.Ratio1")
                    });
                }
            }
            else
            {
                model.Add(new SystemWarningModel
                {
                    Level = SystemWarningLevel.Fail,
                    Text = _localizationService.GetResource("Admin.System.Warnings.DefaultDimension.NotSet")
                });
            }

            //shipping rate coputation methods
            var srcMethods = _shippingService.LoadActiveShippingRateComputationMethods();
            if (!srcMethods.Any())
                model.Add(new SystemWarningModel
                {
                    Level = SystemWarningLevel.Fail,
                    Text = _localizationService.GetResource("Admin.System.Warnings.Shipping.NoComputationMethods")
                });
            if (srcMethods.Count(x => x.ShippingRateComputationMethodType == ShippingRateComputationMethodType.Offline) > 1)
                model.Add(new SystemWarningModel
                {
                    Level = SystemWarningLevel.Warning,
                    Text = _localizationService.GetResource("Admin.System.Warnings.Shipping.OnlyOneOffline")
                });

            //payment methods
            if (_paymentService.LoadActivePaymentMethods().Any())
                model.Add(new SystemWarningModel
                {
                    Level = SystemWarningLevel.Pass,
                    Text = _localizationService.GetResource("Admin.System.Warnings.PaymentMethods.OK")
                });
            else
                model.Add(new SystemWarningModel
                {
                    Level = SystemWarningLevel.Fail,
                    Text = _localizationService.GetResource("Admin.System.Warnings.PaymentMethods.NoActive")
                });

            //incompatible plugins
            if (PluginManager.IncompatiblePlugins != null)
                foreach (var pluginName in PluginManager.IncompatiblePlugins)
                    model.Add(new SystemWarningModel
                    {
                        Level = SystemWarningLevel.Warning,
                        Text = string.Format(_localizationService.GetResource("Admin.System.Warnings.IncompatiblePlugin"), pluginName)
                    });

            //performance settings
            if (!_catalogSettings.IgnoreStoreLimitations && _storeService.GetAllStores().Count == 1)
            {
                model.Add(new SystemWarningModel
                {
                    Level = SystemWarningLevel.Warning,
                    Text = _localizationService.GetResource("Admin.System.Warnings.Performance.IgnoreStoreLimitations")
                });
            }
            if (!_catalogSettings.IgnoreAcl)
            {
                model.Add(new SystemWarningModel
                {
                    Level = SystemWarningLevel.Warning,
                    Text = _localizationService.GetResource("Admin.System.Warnings.Performance.IgnoreAcl")
                });
            }

            //validate write permissions (the same procedure like during installation)
            //TASK 8.3 - OperatingSystem.IsWindows() guard. This is the fix task 6.5 recommended
            //(runtime-deferrals.md section 18.4) and task 7.3 applied at the two equivalent
            //InstallController sites, and it is a real correction rather than warning suppression:
            //FilePermissionHelper.CheckPermissions is annotated [SupportedOSPlatform("windows")]
            //because it reads NTFS ACLs, and WindowsIdentity.GetCurrent() below sits OUTSIDE that
            //method's swallowing try/catch, so on Linux this page THREW instead of rendering. The
            //analyser recognises the guard, so the CA1416 warnings clear as a side effect.
            var dirPermissionsOk = true;
            if (OperatingSystem.IsWindows())
            {
                var dirsToCheck = FilePermissionHelper.GetDirectoriesWrite();
                foreach (string dir in dirsToCheck)
                    if (!FilePermissionHelper.CheckPermissions(dir, false, true, true, false))
                    {
                        model.Add(new SystemWarningModel
                        {
                            Level = SystemWarningLevel.Warning,
                            Text = string.Format(_localizationService.GetResource("Admin.System.Warnings.DirectoryPermission.Wrong"), WindowsIdentity.GetCurrent().Name, dir)
                        });
                        dirPermissionsOk = false;
                    }
                if (dirPermissionsOk)
                    model.Add(new SystemWarningModel
                    {
                        Level = SystemWarningLevel.Pass,
                        Text = _localizationService.GetResource("Admin.System.Warnings.DirectoryPermission.OK")
                    });

                var filePermissionsOk = true;
                var filesToCheck = FilePermissionHelper.GetFilesWrite();
                foreach (string file in filesToCheck)
                    if (!FilePermissionHelper.CheckPermissions(file, false, true, true, true))
                    {
                        model.Add(new SystemWarningModel
                        {
                            Level = SystemWarningLevel.Warning,
                            Text = string.Format(_localizationService.GetResource("Admin.System.Warnings.FilePermission.Wrong"), WindowsIdentity.GetCurrent().Name, file)
                        });
                        filePermissionsOk = false;
                    }
                if (filePermissionsOk)
                    model.Add(new SystemWarningModel
                    {
                        Level = SystemWarningLevel.Pass,
                        Text = _localizationService.GetResource("Admin.System.Warnings.FilePermission.OK")
                    });
            }

            //TASK 8.3 - THE <machineKey> WARNING WAS REMOVED. This is a REMOVAL, not a
            //configuration migration, and appsettings.json must NOT grow a machineKey key.
            //
            //3.90 read ConfigurationManager.GetSection("system.web/machineKey") as
            //System.Web.Configuration.MachineKeySection and warned when the decryption key was
            //auto-generated, because an auto-generated key is per-machine and therefore breaks
            //forms-authentication tickets and view state across a web farm.
            //
            //MachineKeySection does not exist on .NET in any form, and <machineKey> has no
            //successor setting: ASP.NET Core replaced the whole mechanism with Data Protection,
            //whose key ring is a file/registry/blob store configured in code
            //(PersistKeysToFileSystem / ...ToAzureBlobStorage / ...), not a config section with a
            //literal key in it. There is nothing to read and nothing to translate.
            //
            //The underlying operational risk is REAL and is already recorded as part of deferral
            //7.13: a multi-instance deployment must share the Data Protection key ring or auth
            //cookies stop validating across instances - the same class of problem <machineKey>
            //existed to solve. Surfacing that here would mean asking Data Protection which
            //IXmlRepository is in use and whether it is machine-local, which is a genuinely
            //different diagnostic and a new feature. Recorded as deferral 8.3-1 and left to the
            //owner of this page's configuration story (task 8.7) rather than invented here.
            //The Admin.System.Warnings.MachineKey.NotSpecified / .Specified localization
            //resources become orphaned, which is harmless.

            return View(model);
        }
        
        public virtual ActionResult Maintenance()
        {
            if (!_permissionService.Authorize(StandardPermissionProvider.ManageMaintenance))
                return AccessDeniedView();

            var model = new MaintenanceModel();
            model.DeleteGuests.EndDate = DateTime.UtcNow.AddDays(-7);
            model.DeleteGuests.OnlyWithoutShoppingCart = true;
            model.DeleteAbandonedCarts.OlderThan = DateTime.UtcNow.AddDays(-182);
            return View(model);
        }

        [HttpPost, ActionName("Maintenance")]
        [FormValueRequired("delete-guests")]
        public virtual ActionResult MaintenanceDeleteGuests(MaintenanceModel model)
        {
            if (!_permissionService.Authorize(StandardPermissionProvider.ManageMaintenance))
                return AccessDeniedView();

            DateTime? startDateValue = (model.DeleteGuests.StartDate == null) ? null
                            : (DateTime?)_dateTimeHelper.ConvertToUtcTime(model.DeleteGuests.StartDate.Value, _dateTimeHelper.CurrentTimeZone);

            DateTime? endDateValue = (model.DeleteGuests.EndDate == null) ? null
                            : (DateTime?)_dateTimeHelper.ConvertToUtcTime(model.DeleteGuests.EndDate.Value, _dateTimeHelper.CurrentTimeZone).AddDays(1);

            model.DeleteGuests.NumberOfDeletedCustomers = _customerService.DeleteGuestCustomers(startDateValue, endDateValue, model.DeleteGuests.OnlyWithoutShoppingCart);

            return View(model);
        }

        [HttpPost, ActionName("Maintenance")]
        [FormValueRequired("delete-abondoned-carts")]
        public virtual ActionResult MaintenanceDeleteAbandonedCarts(MaintenanceModel model)
        {
            if (!_permissionService.Authorize(StandardPermissionProvider.ManageMaintenance))
                return AccessDeniedView();

            var olderThanDateValue = _dateTimeHelper.ConvertToUtcTime(model.DeleteAbandonedCarts.OlderThan, _dateTimeHelper.CurrentTimeZone);

            model.DeleteAbandonedCarts.NumberOfDeletedItems = _shoppingCartService.DeleteExpiredShoppingCartItems(olderThanDateValue);
            return View(model);
        }

        [HttpPost, ActionName("Maintenance")]
        [FormValueRequired("delete-exported-files")]
        public virtual ActionResult MaintenanceDeleteFiles(MaintenanceModel model)
        {
            if (!_permissionService.Authorize(StandardPermissionProvider.ManageMaintenance))
                return AccessDeniedView();

            DateTime? startDateValue = (model.DeleteExportedFiles.StartDate == null) ? null
                            : (DateTime?)_dateTimeHelper.ConvertToUtcTime(model.DeleteExportedFiles.StartDate.Value, _dateTimeHelper.CurrentTimeZone);

            DateTime? endDateValue = (model.DeleteExportedFiles.EndDate == null) ? null
                            : (DateTime?)_dateTimeHelper.ConvertToUtcTime(model.DeleteExportedFiles.EndDate.Value, _dateTimeHelper.CurrentTimeZone).AddDays(1);


            model.DeleteExportedFiles.NumberOfDeletedFiles = 0;
            //TASK 8.3 - HttpRequest.PhysicalApplicationPath -> CommonHelper.MapPath("~/"), which
            //resolves against CommonHelper.BaseDirectory (the content root, assigned by task 7.2 -
            //deferral 1.5). Same substitution task 4.2 made in MaintenanceService and task 7.3 made
            //in CommonModelFactory's favicon lookup.
            //THE PATH CASING IS ALSO CORRECTED: the directory on disk is Content/files/ExportImport,
            //and the "content\\files\\exportimport" spelling here resolved only on a
            //case-INsensitive filesystem. This is the class of defect task 7.7 found 8 instances of
            //(runtime-deferrals.md section 42.4); the backslash separators are replaced by
            //Path.Combine segments for the same reason.
            string path = Path.Combine(CommonHelper.MapPath("~/"), "Content", "files", "ExportImport");
            foreach (var fullPath in Directory.GetFiles(path))
            {
                try
                {
                    var fileName = Path.GetFileName(fullPath);
                    if (fileName.Equals("index.htm", StringComparison.InvariantCultureIgnoreCase))
                        continue;

                    var info = new FileInfo(fullPath);
                    if ((!startDateValue.HasValue || startDateValue.Value < info.CreationTimeUtc) &&
                        (!endDateValue.HasValue || info.CreationTimeUtc < endDateValue.Value))
                    {
                        System.IO.File.Delete(fullPath);
                        model.DeleteExportedFiles.NumberOfDeletedFiles++;
                    }
                }
                catch (Exception exc)
                {
                    ErrorNotification(exc, false);
                }
            }

            return View(model);
        }

        [HttpPost]
        public virtual ActionResult BackupFiles(DataSourceRequest command)
        {
            if (!_permissionService.Authorize(StandardPermissionProvider.ManageMaintenance))
                return AccessDeniedKendoGridJson();

            var backupFiles = _maintenanceService.GetAllBackupFiles().ToList();
            
            var gridModel = new DataSourceResult
            {
                //TASK 8.3 - the download link now points at the BackupFileDownload action below
                //rather than at a static-file URL. See that action's remarks for why.
                Data = backupFiles.Select(p=>new {p.Name,
                    Length = string.Format("{0:F2} Mb", p.Length / 1024f / 1024f),
                    Link = Url.Action("BackupFileDownload", "Common", new { area = "Admin", fileName = p.Name })
                }),
                Total = backupFiles.Count
            };
            return Json(gridModel);
        }

        /// <summary>
        /// Stream a database backup file to the caller.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>TASK 8.3 — NEW ACTION, replacing a static-file URL that is now dead by design.</b>
        /// 3.90 built the grid link as
        /// <c>_webHelper.GetStoreLocation(false) + "Administration/db_backups/" + p.Name</c> and
        /// relied on <c>System.Web</c>'s static handler to serve it — which is why
        /// <c>Nop.Web/Web.config</c> carried
        /// <c>&lt;mimeMap fileExtension=".bak" mimeType="application/octet-stream"/&gt;</c> with the
        /// comment "Allow database backup (.bak) file loading".
        /// </para>
        /// <para>
        /// That URL cannot work now, and deliberately so — it is refused three times over:
        /// task 7.4 did not reproduce the <c>.bak</c> mimeMap (runtime-deferrals.md §33.2),
        /// <c>NopStaticFileProvider</c>'s allow-list excludes <c>Administration/</c> entirely, and
        /// it additionally denies the <c>.bak</c> extension. <b>Serving database backups as
        /// unauthenticated static files was the real defect</b>: in 3.90 anyone who could guess a
        /// backup filename could download the entire database without being signed in, because
        /// static files never entered the MVC pipeline and so never met <c>[AdminAuthorize]</c>.
        /// </para>
        /// <para>
        /// This action therefore inherits the class-level <c>[AdminAuthorize]</c>,
        /// <c>[AdminValidateIpAddress]</c> and <c>[AdminVendorValidation]</c> filters and re-checks
        /// <c>ManageMaintenance</c> explicitly, exactly as every other action on this page does.
        /// The filename is resolved through <c>IMaintenanceService.GetBackupPath</c> and then
        /// checked against the backup directory with a canonicalised prefix test, so a
        /// <c>../</c> traversal cannot escape it.
        /// </para>
        /// </remarks>
        public virtual ActionResult BackupFileDownload(string fileName)
        {
            if (!_permissionService.Authorize(StandardPermissionProvider.ManageMaintenance))
                return AccessDeniedView();

            if (string.IsNullOrWhiteSpace(fileName))
                return NotFound();

            //refuse anything that is not a bare file name before it reaches the filesystem
            if (fileName.IndexOfAny(new[] { '/', '\\' }) >= 0 ||
                fileName.IndexOf("..", StringComparison.Ordinal) >= 0 ||
                fileName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
                return NotFound();

            var backupPath = _maintenanceService.GetBackupPath(fileName);

            //belt and braces: confirm the resolved path is still inside the backup directory
            var backupDirectory = Path.GetFullPath(Path.GetDirectoryName(_maintenanceService.GetBackupPath("x")) ?? string.Empty);
            var fullPath = Path.GetFullPath(backupPath);
            if (!fullPath.StartsWith(backupDirectory + Path.DirectorySeparatorChar, StringComparison.Ordinal))
                return NotFound();

            if (!System.IO.File.Exists(fullPath))
                return NotFound();

            return PhysicalFile(fullPath, MimeTypes.ApplicationOctetStream, fileName);
        }

        [HttpPost, ActionName("Maintenance")]
        [FormValueRequired("backup-database")]
        public virtual ActionResult BackupDatabase(MaintenanceModel model)
        {
            if (!_permissionService.Authorize(StandardPermissionProvider.ManageMaintenance))
                return AccessDeniedView();

            try
            {
                _maintenanceService.BackupDatabase();
                this.SuccessNotification(_localizationService.GetResource("Admin.System.Maintenance.BackupDatabase.BackupCreated"));
            }
            catch (Exception exc)
            {
                ErrorNotification(exc);
            }

            return View(model);
        }

        [HttpPost, ActionName("Maintenance")]
        [FormValueRequired("backupFileName", "action")]
        public virtual ActionResult BackupAction(MaintenanceModel model)
        {
            if (!_permissionService.Authorize(StandardPermissionProvider.ManageMaintenance))
                return AccessDeniedView();

            var action = this.Request.Form["action"];

            var fileName = this.Request.Form["backupFileName"];
            var backupPath = _maintenanceService.GetBackupPath(fileName);

            try
            {
                switch (action)
                {
                    case "delete-backup":
                    {
                        System.IO.File.Delete(backupPath);
                        this.SuccessNotification(string.Format(_localizationService.GetResource("Admin.System.Maintenance.BackupDatabase.BackupDeleted"), fileName));
                    }
                        break;
                    case "restore-backup":
                    {
                        _maintenanceService.RestoreDatabase(backupPath);
                        this.SuccessNotification(_localizationService.GetResource("Admin.System.Maintenance.BackupDatabase.DatabaseRestored"));
                    }
                        break;
                }
            }
            catch (Exception exc)
            {
                ErrorNotification(exc);
            }
            
            return View(model);
        }

        [NopChildActionOnly]
        public virtual ActionResult LanguageSelector()
        {
            var model = new LanguageSelectorModel();
            model.CurrentLanguage = _workContext.WorkingLanguage.ToModel();
            model.AvailableLanguages = _languageService
                .GetAllLanguages(storeId: _storeContext.CurrentStore.Id)
                .Select(x => x.ToModel())
                .ToList();
            return PartialView(model);
        }
        public virtual ActionResult SetLanguage(int langid, string returnUrl = "")
        {
            var language = _languageService.GetLanguageById(langid);
            if (language != null)
            {
                _workContext.WorkingLanguage = language;
            }

            //home page
            if (String.IsNullOrEmpty(returnUrl))
                returnUrl = Url.Action("Index", "Home", new { area = "Admin" });
            //prevent open redirection attack
            if (!Url.IsLocalUrl(returnUrl))
                return RedirectToAction("Index", "Home", new { area = "Admin" });
            return Redirect(returnUrl);
        }

        [HttpPost]
        public virtual ActionResult ClearCache(string returnUrl = "")
        {
            if (!_permissionService.Authorize(StandardPermissionProvider.ManageMaintenance))
                return AccessDeniedView();

            var cacheManager = EngineContext.Current.ContainerManager.Resolve<ICacheManager>("nop_cache_static");
            cacheManager.Clear();

            //home page
            if (String.IsNullOrEmpty(returnUrl))
                return RedirectToAction("Index", "Home", new { area = "Admin" });
            //prevent open redirection attack
            if (!Url.IsLocalUrl(returnUrl))
                return RedirectToAction("Index", "Home", new { area = "Admin" });
            return Redirect(returnUrl);
        }

        [HttpPost]
        public virtual ActionResult RestartApplication(string returnUrl = "")
        {
            if (!_permissionService.Authorize(StandardPermissionProvider.ManageMaintenance))
                return AccessDeniedView();

            //restart application
            _webHelper.RestartAppDomain();

            //home page
            if (String.IsNullOrEmpty(returnUrl))
                return RedirectToAction("Index", "Home", new { area = "Admin" });
            //prevent open redirection attack
            if (!Url.IsLocalUrl(returnUrl))
                return RedirectToAction("Index", "Home", new { area = "Admin" });
            return Redirect(returnUrl);
        }


        public virtual ActionResult SeNames()
        {
            if (!_permissionService.Authorize(StandardPermissionProvider.ManageMaintenance))
                return AccessDeniedView();

            var model = new UrlRecordListModel();
            return View(model);
        }
        [HttpPost]
        public virtual ActionResult SeNames(DataSourceRequest command, UrlRecordListModel model)
        {
            if (!_permissionService.Authorize(StandardPermissionProvider.ManageMaintenance))
                return AccessDeniedKendoGridJson();

            var urlRecords = _urlRecordService.GetAllUrlRecords(model.SeName, command.Page - 1, command.PageSize);
            var gridModel = new DataSourceResult
            {
                Data = urlRecords.Select(x =>
                {
                    //language
                    string languageName;
                    if (x.LanguageId == 0)
                    {
                        languageName = _localizationService.GetResource("Admin.System.SeNames.Language.Standard");
                    }
                    else
                    {
                        var language = _languageService.GetLanguageById(x.LanguageId);
                        languageName = language != null ? language.Name : "Unknown";
                    }

                    //details URL
                    string detailsUrl = "";
                    var entityName = x.EntityName != null ? x.EntityName.ToLowerInvariant() : "";
                    switch (entityName)
                    {
                        case "blogpost":
                            detailsUrl = Url.Action("Edit", "Blog", new { id = x.EntityId });
                            break;
                        case "category":
                            detailsUrl = Url.Action("Edit", "Category", new { id = x.EntityId });
                            break;
                        case "manufacturer":
                            detailsUrl = Url.Action("Edit", "Manufacturer", new { id = x.EntityId });
                            break;
                        case "product":
                            detailsUrl = Url.Action("Edit", "Product", new { id = x.EntityId });
                            break;
                        case "newsitem":
                            detailsUrl = Url.Action("Edit", "News", new { id = x.EntityId });
                            break;
                        case "topic":
                            detailsUrl = Url.Action("Edit", "Topic", new { id = x.EntityId });
                            break;
                        case "vendor":
                            detailsUrl = Url.Action("Edit", "Vendor", new { id = x.EntityId });
                            break;
                        default:
                            break;
                    }

                    return new UrlRecordModel
                    {
                        Id = x.Id,
                        Name = x.Slug,
                        EntityId = x.EntityId,
                        EntityName = x.EntityName,
                        IsActive = x.IsActive,
                        Language = languageName,
                        DetailsUrl = detailsUrl
                    };
                }),
                Total = urlRecords.TotalCount
            };
            return Json(gridModel);
        }
        [HttpPost]
        public virtual ActionResult DeleteSelectedSeNames(ICollection<int> selectedIds)
        {
            if (!_permissionService.Authorize(StandardPermissionProvider.ManageMaintenance))
                return AccessDeniedView();

            if (selectedIds != null)
            {
                _urlRecordService.DeleteUrlRecords(_urlRecordService.GetUrlRecordsByIds(selectedIds.ToArray()));
            }

            return Json(new { Result = true });
        }


        [NopChildActionOnly]
        public virtual ActionResult PopularSearchTermsReport()
        {
            if (!_permissionService.Authorize(StandardPermissionProvider.ManageProducts))
                return Content("");

            return PartialView();
        }
        [HttpPost]
        public virtual ActionResult PopularSearchTermsReport(DataSourceRequest command)
        {
            if (!_permissionService.Authorize(StandardPermissionProvider.ManageProducts))
                return AccessDeniedKendoGridJson();

            var searchTermRecordLines = _searchTermService.GetStats(command.Page - 1, command.PageSize);
            var gridModel = new DataSourceResult
            {
                Data = searchTermRecordLines.Select(x => new SearchTermReportLineModel
                {
                    Keyword = x.Keyword,
                    Count = x.Count,
                }),
                Total = searchTermRecordLines.TotalCount
            };
            return Json(gridModel);
        }


        //action displaying notification (warning) to a store owner that "limit per store" feature is ignored
        [NopChildActionOnly]
        public virtual ActionResult MultistoreDisabledWarning()
        {
            //default setting
            bool enabled = _catalogSettings.IgnoreStoreLimitations;
            if (!enabled)
            {
                //overridden settings
                var stores = _storeService.GetAllStores();
                foreach (var store in stores)
                {
                    if (!enabled)
                    {
                        var catalogSettings = _settingService.LoadSetting<CatalogSettings>(store.Id);
                        enabled = catalogSettings.IgnoreStoreLimitations;
                    }
                }
            }

            //This setting is disabled. No warnings.
            if (!enabled)
                return Content("");

            return PartialView();
        }
        //action displaying notification (warning) to a store owner that "ACL rules" feature is ignored
        [NopChildActionOnly]
        public virtual ActionResult AclDisabledWarning()
        {
            //default setting
            bool enabled = _catalogSettings.IgnoreAcl;
            if (!enabled)
            {
                //overridden settings
                var stores = _storeService.GetAllStores();
                foreach (var store in stores)
                {
                    if (!enabled)
                    {
                        var catalogSettings = _settingService.LoadSetting<CatalogSettings>(store.Id);
                        enabled = catalogSettings.IgnoreAcl;
                    }
                }
            }

            //This setting is disabled. No warnings.
            if (!enabled)
                return Content("");

            return PartialView();
        }

        //action displaying notification (warning) to a store owner that entered SE URL already exists
        public virtual ActionResult UrlReservedWarning(string entityId, string entityName, string seName)
        {
            if (string.IsNullOrEmpty(seName))
                return Json(new { Result = string.Empty });

            int parsedEntityId;
            int.TryParse(entityId, out parsedEntityId);
            var validatedSeName = SeoExtensions.ValidateSeName(parsedEntityId, entityName, seName, null, false);

            if (seName.Equals(validatedSeName, StringComparison.InvariantCultureIgnoreCase))
                return Json(new { Result = string.Empty });

            return Json(new { Result = string.Format(_localizationService.GetResource("Admin.System.Warnings.URL.Reserved"), validatedSeName) });
        }

        #endregion
    }
}
