using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EmailService;
using IdentityByExamples.CustomTokenProviders;
using IdentityByExamples.CustomValidators;
using IdentityByExamples.Factory;
using IdentityByExamples.Models;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace IdentityByExamples
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            //added an existing email sender to this project with user secrets
            var emailConfig = Configuration
                .GetSection("EmailConfiguration")
                .Get<EmailConfiguration>();
            services.AddSingleton(emailConfig);
            services.AddScoped<IEmailSender, EmailSender>();

            services.AddDbContext<ApplicationContext>(opts =>
                opts.UseSqlServer(Configuration.GetConnectionString("sqlConnection")));

            services.AddIdentity<User, IdentityRole>(opt =>
                {
                    opt.Password.RequiredLength = 8;
                    opt.Password.RequireDigit = false;
                    opt.Password.RequireUppercase = false;
                    opt.User.RequireUniqueEmail = true;

                    opt.SignIn.RequireConfirmedEmail = true;

                    opt.Tokens.EmailConfirmationTokenProvider = "emailconfirmation";

                    // default lockout policy
                    opt.Lockout.AllowedForNewUsers = true;
                    opt.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(2);
                    opt.Lockout.MaxFailedAccessAttempts = 3;
                })
                .AddEntityFrameworkStores<ApplicationContext>()
                .AddDefaultTokenProviders() //add the required token providers to enable the token generation in our project. Also in order to set token time limit we added the one below
                .AddTokenProvider<EmailConfirmationTokenProvider<User>>("emailconfirmation")
                .AddPasswordValidator<CustomPasswordValidator<User>>();

            services.Configure<DataProtectionTokenProviderOptions>(opt =>//this for reset password 
               opt.TokenLifespan = TimeSpan.FromHours(2));

            services.Configure<EmailConfirmationTokenProviderOptions>(opt => // this for email confirmation, a custom token provider
               opt.TokenLifespan = TimeSpan.FromDays(3));

            services.AddAutoMapper(typeof(Startup));
            //services.ConfigureApplicationCookie(o => o.LoginPath = "/Authentication/Login"); // if we want to change the default login url /account/login to smth else, write it here and add proper controller and views.
            services.AddScoped<IUserClaimsPrincipalFactory<User>, CustomClaimsFactory>();

            services.AddAuthentication()
                .AddGoogle("google", opt =>
                {
                    var googleAuth = Configuration.GetSection("Authentication:Google");

                    opt.ClientId = googleAuth["ClientId"];
                    opt.ClientSecret = googleAuth["ClientSecret"];
                    opt.SignInScheme = IdentityConstants.ExternalScheme;
                });

            services.AddControllersWithViews();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }
            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{controller=Home}/{action=Index}/{id?}");
            });
        }
    }
}
