using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ViewHTab
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

            app.UseAuthorization();

            Infobase.engine = new RDFEngine.REngine();
            Infobase.engine.Load(RDFEngine.PhototekaGenerator.Generate(1000));

            // ============= Загрузка базы данных из текста модели
            if (true)
            {
                Infobase.engine.Clear();
                string graphModelText = @"<?xml version='1.0' encoding='utf-8'?>
<rdf:RDF xmlns:rdf='http://www.w3.org/1999/02/22-rdf-syntax-ns#'>
  <person rdf:about='p3817'>
    <name xml:lang='ru'>Иванов</name>
    <from-date>1988</from-date>
  </person>
  <person rdf:about='p3818'>
    <from-date>1999</from-date>
    <name xml:lang='ru'>Петров</name>
  </person>
  <org-sys rdf:about='o19302'>
    <from-date>1959</from-date>
    <name>НГУ</name>
  </org-sys>
  <participation rdf:about='r1111'>
    <participant rdf:resource='p3817' />
    <in-org rdf:resource='o19302' />
    <role>профессор</role>
  </participation>
  <participation rdf:about='r1112'>
    <participant rdf:resource='p3818' />
    <in-org rdf:resource='o19302' />
    <from-date>2008</from-date>
    <role>ассистент</role>
  </participation>
</rdf:RDF>";
                System.Xml.Linq.XElement graphModelXml = System.Xml.Linq.XElement.Parse(graphModelText);
                Infobase.engine.Load(graphModelXml.Elements());
            }
            Infobase.engine.Build();
            Infobase.ront = new RDFEngine.ROntology(); // тестовая онтология

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{controller=Home}/{action=Index}/{id?}");
            });
        }
    }
}
