# Credits

Example copied originally from https://github.com/open-telemetry/opentelemetry-dotnet

## Getting started

Visit our online documentation at https://docs.immersivefusion.com

Instumentation documentation can be found at https://docs.immersivefusion.com/instrument/

Visualization documentation can be found at https://docs.immersivefusion.com/visualize/

The samples require a valid Immersive Fusion license. To get started with a basic (forever-trial) visit https://my.immersivefusion.com/getting-started

## Elevated access

You may need to run Visual Studio as administrator to allow the the samples to bind to local ports.

## Credentials

You will need to supply your own credentials for RabbitMq. 

### Local instance of Immersive APM

If you have a local instance license only, you can override direct hub credentials in all the sample using similar code:

```

   fusionOptions.DirectConnection = new DirectConnectionInformation
                    {
                        Name = "Demo.AspNetCore",
                        Uri = new Uri("amqp://localhost"),
                        Tls = false,
                        IgnoreTlsErrors = true,
                        UserName = "ifdev",
                        Password = "password1",
                        Exchange = "local-dev"
                    };
```


