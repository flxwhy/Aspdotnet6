global using Tool;
using AlpathAny;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NLog;
using NLog.Web;
using PlatData;

var builder = WebApplication.CreateBuilder(args);

//��ȡָ������·��
builder.Host.ConfigureAppConfiguration(app =>
{
    app.AddKeyPerFile(directoryPath:"/run/secrets",optional: true);
});

string connectstr = null;
connectstr = builder.Configuration["Movies:ConnectionString"];
//��֪docker��Կ��Ϣ
if (connectstr == null) {
    var sercertstr = builder.Configuration["Movies_ServiceApiKey"];
    if (!string.IsNullOrEmpty(sercertstr))
        connectstr = JsonConvert.DeserializeObject<JToken>(sercertstr).GetTrueValue<string>("mysqlconnectstr");
}
    
   


ConfigurationValue = builder.Configuration["testone"];
//���ؼ�Ȩ��ַ
AppraisalUrl = builder.Configuration["Appraisalurl"];
DefaultRecturl = builder.Configuration["DefaultRecturl"];

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

//redis����
var sectionredis = builder.Configuration.GetSection("Redis:Default");
string redisconnectionString = sectionredis.GetSection("Connection").Value;
string redisinstanceName = sectionredis.GetSection("InstanceName").Value;
string redissyscustomkey = sectionredis.GetSection("SysCustomKey").Value;
int redisdefaultDB = int.Parse(sectionredis.GetSection("DefaultDB").Value ?? "0");

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
    builder.Register(c=>new RedisHelper(redisconnectionString, redisinstanceName, redissyscustomkey,redisdefaultDB)).AsSelf().InstancePerLifetimeScope();
    //��ģ�����ע��    
    builder.RegisterModule<AutofacModuleRegister>();
});
//����������ʵ������
builder.Services.AddControllersWithViews().AddControllersAsServices();

// Add services to the container.
builder.Services.AddRazorPages();

builder.Services.AddAuthentication("Bearer").AddIdentityServerAuthentication(x =>
{
    x.Authority = AppraisalUrl;//��Ȩ�����ַ
    x.RequireHttpsMetadata = false;
    x.ApiName = "api";//��Ȩ��Χ
});

//AddHttpClient ��ע�� IHttpClientFactory
builder.Services.AddHttpClient();

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
        pattern: "{controller=Login}/{action=Login}/{id?}");
});

app.Run();


partial class Program {

    public static string? ConfigurationValue { get; private set; }

    /// <summary>
    /// ��Ȩ��ַ
    /// </summary>
    public static string AppraisalUrl { get; private set; }

    /// <summary>
    /// Ĭ����ת��ַ
    /// </summary>
    public static string DefaultRecturl { get; private set; }
}
