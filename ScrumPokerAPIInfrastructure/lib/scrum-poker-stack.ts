import * as cdk from 'aws-cdk-lib';
import * as apigwv2 from 'aws-cdk-lib/aws-apigatewayv2';
import * as apigwv2Integrations from 'aws-cdk-lib/aws-apigatewayv2-integrations';
import * as lambda from 'aws-cdk-lib/aws-lambda';
import * as logs from 'aws-cdk-lib/aws-logs';
import * as secretsmanager from 'aws-cdk-lib/aws-secretsmanager';
import { Construct } from 'constructs';
import * as path from 'path';

export class ScrumPokerStack extends cdk.Stack {
  public readonly webSocketUrl: string;

  constructor(scope: Construct, id: string, props?: cdk.StackProps) {
    super(scope, id, props);

    const cwd = process.cwd();
    const upToRepo = path.basename(cwd) === 'ScrumPokerAPIInfrastructure' ? '..' : '.';
    const scrumPokerApiPath = path.resolve(cwd, upToRepo, 'ScrumPokerAPI');

    const databaseSecret = new secretsmanager.Secret(this, 'DatabaseSecret', {
      description: 'PostgreSQL connection string for Scrum Poker API (plain text Npgsql format)',
      secretStringValue: cdk.SecretValue.unsafePlainText(
        'Host=YOUR_HOST;Port=5432;Database=scrumpoker;Username=YOUR_USER;Password=YOUR_PASSWORD',
      ),
    });

    const handler = new lambda.Function(this, 'ScrumPokerHandler', {
      functionName: 'ScrumPokerApi',
      runtime: lambda.Runtime.DOTNET_8,
      handler: 'ScrumPokerAPI::ScrumPokerAPI.LambdaEntryPoint::FunctionHandler',
      memorySize: 512,
      timeout: cdk.Duration.seconds(30),
      logRetention: logs.RetentionDays.TWO_WEEKS,
      environment: {
        DATABASE_SECRET_ARN: databaseSecret.secretArn,
      },
      code: lambda.Code.fromAsset(scrumPokerApiPath, {
        bundling: {
          image: lambda.Runtime.DOTNET_8.bundlingImage,
          command: [
            'bash',
            '-c',
            [
              'dotnet restore ScrumPokerAPI.csproj',
              'dotnet publish ScrumPokerAPI.csproj -c Release -r linux-x64 --self-contained false -o /asset-output',
            ].join(' && '),
          ],
          user: 'root',
        },
      }),
    });

    databaseSecret.grantRead(handler);

    const webSocketApi = new apigwv2.WebSocketApi(this, 'ScrumPokerWs', {
      apiName: 'scrum-poker-ws',
      description: 'Scrum Poker WebSocket API',
      connectRouteOptions: {
        integration: new apigwv2Integrations.WebSocketLambdaIntegration('Connect', handler),
      },
      disconnectRouteOptions: {
        integration: new apigwv2Integrations.WebSocketLambdaIntegration('Disconnect', handler),
      },
      defaultRouteOptions: {
        integration: new apigwv2Integrations.WebSocketLambdaIntegration('Default', handler),
      },
    });

    const stage = new apigwv2.WebSocketStage(this, 'ProdStage', {
      webSocketApi,
      stageName: 'prod',
      autoDeploy: true,
    });

    webSocketApi.grantManageConnections(handler);

    this.webSocketUrl = stage.url;

    new cdk.CfnOutput(this, 'WebSocketUrl', {
      description: 'Connect your frontend with wss (use this URL in the browser)',
      value: stage.url,
    });

    new cdk.CfnOutput(this, 'DatabaseSecretArn', {
      description: 'Update this secret in Secrets Manager with your real PostgreSQL connection string',
      value: databaseSecret.secretArn,
    });

    new cdk.CfnOutput(this, 'LambdaFunctionArn', {
      value: handler.functionArn,
    });
  }
}
