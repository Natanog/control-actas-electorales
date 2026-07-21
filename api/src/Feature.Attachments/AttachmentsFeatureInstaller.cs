using Feature.Attachments.Actas.Ocr;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Feature.Attachments;

public static class AttachmentsFeatureInstaller
{
    public static IServiceCollection AddAttachmentsFeature(this IServiceCollection services, IConfiguration? configuration = null)
    {
        if (configuration is not null)
        {
            services.Configure<ActaOcrOptions>(configuration.GetSection(ActaOcrOptions.SectionKey));
        }
        else
        {
            services.Configure<ActaOcrOptions>(options =>
            {
                options.Enabled = bool.TryParse(Environment.GetEnvironmentVariable("ACTA_OCR_ENABLED"), out var enabled) && enabled;
                options.BaseUrl = Environment.GetEnvironmentVariable("ACTA_OCR_BASE_URL") ?? "http://acta-ocr:8000";
                options.ReviewThreshold = decimal.TryParse(Environment.GetEnvironmentVariable("ACTA_OCR_REVIEW_THRESHOLD"), out var threshold)
                    ? threshold : 0.85m;
            });
        }

        services.AddHttpClient<HttpActaOcrProvider>((sp, client) =>
        {
            var options = sp.GetRequiredService<IOptions<ActaOcrOptions>>().Value;
            client.BaseAddress = new Uri(options.BaseUrl);
            client.Timeout = TimeSpan.FromMinutes(2);
        });
        services.AddScoped<IActaOcrProvider>(sp =>
        {
            var options = sp.GetRequiredService<IOptions<ActaOcrOptions>>().Value;
            return options.Enabled ? sp.GetRequiredService<HttpActaOcrProvider>() : new DisabledActaOcrProvider();
        });
        return services;
    }
}
