using Feature.Attachments.Actas.Ocr;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Feature.Attachments;

public static class AttachmentsFeatureInstaller
{
    public static IServiceCollection AddAttachmentsFeature(this IServiceCollection services, IConfiguration? configuration = null)
    {
        var section = configuration?.GetSection(ActaOcrOptions.SectionKey);
        if (section is not null) services.Configure<ActaOcrOptions>(section);
        else services.Configure<ActaOcrOptions>(_ => { });

        services.AddHttpClient<HttpActaOcrProvider>((sp, client) =>
        {
            var options = sp.GetRequiredService<IOptions<ActaOcrOptions>>().Value;
            client.BaseAddress = new Uri(options.BaseUrl);
            client.Timeout = TimeSpan.FromMinutes(2);
        });
        services.AddScoped<IActaOcrProvider>(sp =>
        {
            var options = sp.GetRequiredService<IOptions<ActaOcrOptions>>().Value;
            return options.Enabled
                ? sp.GetRequiredService<HttpActaOcrProvider>()
                : new DisabledActaOcrProvider();
        });
        return services;
    }
}
