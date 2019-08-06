using System.Diagnostics;
using chess.engine;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;

[assembly: FunctionsStartup(typeof(chess.functions.Startup))]
namespace chess.functions
{
    public class Startup : FunctionsStartup
    {
        public override void Configure(IFunctionsHostBuilder builder)
        {
            //            Debug.Assert(builder != null, "Builder is null");
            Debug.Assert(builder.Services != null, "Builder.Services is null");

            builder.Services.AddChessDependencies();
        }
    }
}