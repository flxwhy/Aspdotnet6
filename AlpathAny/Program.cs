using AlpathAny;
using Autofac;
using Autofac.Core;
using Autofac.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using NLog;
using NLog.Extensions.Logging;
using NLog.Web;
using OperateService.Iservice;
using OperateService.ITableService;
using OperateService.Service;
using OperateService.TableService;
using PlatData;

var builder = WebApplication.CreateBuilder(args);
var movieApiKey = builder.Configuration["Movies:ConnectionString"];
var connectstr = movieApiKey;
    //builder.Configuration.GetConnectionString("AlanConnection");

ConfigurationValue = builder.Configuration["testone"];
//���ؼ�Ȩ��ַ
Appraisalurl = builder.Configuration["Appraisalurl"];

//builder.Services.AddDbContext<DbTContext>(options => options.UseMySql(connectstr, MySqlServerVersion.LatestSupportedServerVersion));
//builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
//builder.Services.AddTransient<ISysAdmin, SysAdminService>();


//��ʼ����־���
var logger = LogManager.Setup().RegisterNLogWeb().GetCurrentClassLogger();

builder.Host.ConfigureLogging(logging =>
{
    logging.ClearProviders();
    logging.SetMinimumLevel(Microsoft.Extensions.Logging.LogLevel.Trace);
}).UseNLog();

builder.Host.UseServiceProviderFactory(new AutofacServiceProviderFactory());
builder.Host.ConfigureContainer<ContainerBuilder>(builder => {
    builder.Register(c =>
    {
        //����mysql
        var optionsBuilder = new DbContextOptionsBuilder<DbTContext>();
        optionsBuilder.UseMySql(connectstr, MySqlServerVersion.LatestSupportedServerVersion);
        return optionsBuilder.Options;
    }).InstancePerLifetimeScope();
    builder.RegisterType<DbTContext>().AsSelf().InstancePerLifetimeScope();
    //��ģ�����ע��    
    builder.RegisterModule<AutofacModuleRegister>();
});
//����������ʵ������
builder.Services.AddControllersWithViews().AddControllersAsServices();

// Add services to the container.
builder.Services.AddRazorPages();

builder.Services.AddAuthentication("Bearer").AddIdentityServerAuthentication(x =>
{
    x.Authority = Appraisalurl;//��Ȩ�����ַ
    x.RequireHttpsMetadata = false;
    x.ApiName = "api";//��Ȩ��Χ
});

var app = builder.Build();

//��ʼ�����ݿ�
using (var scope = AutofacModuleRegister.GetContainer().BeginLifetimeScope())
{
    var dbint= scope.Resolve<DbTContext>();
    dbint.Database.EnsureCreated();
}


//��һ��Run��ʼ�����ݿ�
//using (var servicescop=app.Services.CreateScope())
//{
//    var services = servicescop.ServiceProvider;
//    var tdatabast = services.GetRequiredService<DbTContext>();
//    tdatabast.Database.EnsureCreated();
//}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapRazorPages();

app.UseEndpoints(endpoints =>
{
    endpoints.MapControllerRoute(
      name: "areas",
      pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}"
    );
    endpoints.MapControllerRoute(
        name: "default",
        pattern: "{controller=Forward}/{action=Index}/{id?}");
});

app.Run();


partial class Program {

    public static string? ConfigurationValue { get; private set; }

    /// <summary>
    /// ��Ȩ��ַ
    /// </summary>
    public static string Appraisalurl { get; private set; }
}
