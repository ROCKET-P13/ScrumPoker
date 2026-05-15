import * as path from 'path';

import * as cdk from 'aws-cdk-lib';
import * as apigwv2 from 'aws-cdk-lib/aws-apigatewayv2';
import * as apigwv2Integrations from 'aws-cdk-lib/aws-apigatewayv2-integrations';
import * as lambda from 'aws-cdk-lib/aws-lambda';
import * as secretsmanager from 'aws-cdk-lib/aws-secretsmanager';
import { Construct } from 'constructs';

export class ScrumPokerStack extends cdk.Stack {
	public readonly webSocketUrl: string;

	constructor (scope: Construct, id: string, props?: cdk.StackProps) {
		super(scope, id, props);

		// Plaintext Npgsql connection string (Supabase pooler recommended for Lambda). Replace in Secrets Manager after deploy.
		const databaseSecret = new secretsmanager.Secret(this, 'SupabaseConnectionSecret', {
			secretName: 'ScrumPokerAPI_SupabaseConnection',
			description: 'Npgsql connection string for Supabase Postgres (pooler URI from Supabase dashboard)',
			removalPolicy: cdk.RemovalPolicy.RETAIN,
			secretStringValue: cdk.SecretValue.unsafePlainText(
				'Host=YOUR_PROJECT.pooler.supabase.com;Port=6543;Username=postgres.YOUR_PROJECT_REF;'
					+ 'Password=YOUR_PASSWORD;Database=postgres;SSL Mode=Require'
			),
		});

		const handler = new lambda.Function(this, 'ScrumPokerHandler', {
			functionName: 'ScrumPokerAPI',
			runtime: lambda.Runtime.DOTNET_8,
			handler: 'ScrumPokerAPI::ScrumPokerAPI.LambdaEntryPoint::FunctionHandler',
			code: lambda.Code.fromAsset(
				path.join(__dirname, '../../ScrumPokerAPI/bin/Release/net8.0/publish')
			),
			timeout: cdk.Duration.seconds(30),
			memorySize: 512,
			environment: {
				DATABASE_SECRET_ARN: databaseSecret.secretArn,
			},
		});

		databaseSecret.grantRead(handler);

		const webSocketApi = new apigwv2.WebSocketApi(this, 'ScrumPokerWebSocket', {
			apiName: 'scrum-poker-ws',
			description: 'Scrum Poker WebSocket API',
			connectRouteOptions: {
				integration: new apigwv2Integrations.WebSocketLambdaIntegration('Connect', handler),
			},
			disconnectRouteOptions: {
				integration: new apigwv2Integrations.WebSocketLambdaIntegration('Disconnect', handler),
			},
			defaultRouteOptions: {
				// Do not use id "Default" here: it collides with the $default route logical id during synth.
				integration: new apigwv2Integrations.WebSocketLambdaIntegration('MessageHandler', handler),
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
			description:
				'Browser WebSocket endpoint (wss://.../prod). Use with new WebSocket(url). '
				+ 'Do not use the HTTPS Callback URL from the console; that is for PostToConnection only.',
			value: stage.url,
		});

		new cdk.CfnOutput(this, 'DatabaseSecretArn', {
			description:
				'Secrets Manager ARN: set the secret value to your Supabase connection string '
				+ '(Transaction pooler / Npgsql from Supabase Project Settings → Database).',
			value: databaseSecret.secretArn,
		});

		new cdk.CfnOutput(this, 'LambdaFunctionArn', {
			value: handler.functionArn,
		});
	}
}
