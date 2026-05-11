import * as path from 'path';

import * as cdk from 'aws-cdk-lib';
import * as apigwv2 from 'aws-cdk-lib/aws-apigatewayv2';
import * as apigwv2Integrations from 'aws-cdk-lib/aws-apigatewayv2-integrations';
import * as ec2 from 'aws-cdk-lib/aws-ec2';
import * as lambda from 'aws-cdk-lib/aws-lambda';
import * as rds from 'aws-cdk-lib/aws-rds';
import * as secretsmanager from 'aws-cdk-lib/aws-secretsmanager';
import { Construct } from 'constructs';

export class ScrumPokerStack extends cdk.Stack {
	public readonly webSocketUrl: string;

	constructor (scope: Construct, id: string, props?: cdk.StackProps) {
		super(scope, id, props);

		const vpc = new ec2.Vpc(this, 'ServerlessVpc', {
			maxAzs: 2,
			subnetConfiguration: [
				{
					name: 'Public',
					subnetType: ec2.SubnetType.PUBLIC,
					cidrMask: 24,
				},
				{
					name: 'Private',
					subnetType: ec2.SubnetType.PRIVATE_WITH_EGRESS,
					cidrMask: 24,
				},
			],
		});

		const dbSecurityGroup = new ec2.SecurityGroup(this, 'DbSecurityGroup', { vpc, allowAllOutbound: true });
		const lambdaSecurityGroup = new ec2.SecurityGroup(this, 'LambdaSecurityGroup', { vpc, allowAllOutbound: true });
		dbSecurityGroup.addIngressRule(lambdaSecurityGroup, ec2.Port.tcp(5432), 'Allow Lambda to reach Postgres');

		const databaseSecret = new secretsmanager.Secret(this, 'DatabaseSecret', {
			secretName: 'ScrumPokerAPI_Secret',
			description: 'PostgreSQL connection string for Scrum Poker API (plain text Npgsql format)',
			generateSecretString: {
				secretStringTemplate: JSON.stringify({ username: 'scrumpokerapi' }),
				generateStringKey: 'password',
				excludeCharacters: `;/@": %$><'`,
				passwordLength: 16,
			},
		});

		const dbInstance = new rds.DatabaseInstance(this, 'PostgresDb', {
			engine: rds.DatabaseInstanceEngine.postgres({
				version: rds.PostgresEngineVersion.VER_17_2,
			}),
			vpc,
			credentials: rds.Credentials.fromSecret(databaseSecret),
			vpcSubnets: { subnetType: ec2.SubnetType.PRIVATE_WITH_EGRESS },
			securityGroups: [dbSecurityGroup],
			allocatedStorage: 20,
			maxAllocatedStorage: 100,
			publiclyAccessible: false,
			databaseName: 'scrumpoker',
			removalPolicy: cdk.RemovalPolicy.DESTROY,
		});

		const handler = new lambda.Function(this, 'ScrumPokerHandler', {
			functionName: 'ScrumPokerAPI',
			runtime: lambda.Runtime.DOTNET_8,
			handler: 'ScrumPokerAPI::ScrumPokerAPI.LambdaEntryPoint::FunctionHandler',
			code: lambda.Code.fromAsset(
				path.join(__dirname, '../../ScrumPokerAPI/bin/Release/net8.0/publish')
			),
			vpc,
			timeout: cdk.Duration.seconds(30),
			memorySize: 512,
			securityGroups: [lambdaSecurityGroup],
			vpcSubnets: { subnetType: ec2.SubnetType.PRIVATE_WITH_EGRESS },
			environment: {
				POSTGRES_SECRET_ARN: databaseSecret.secretArn,
				DB_HOST: dbInstance.dbInstanceEndpointAddress,
				DB_PORT: dbInstance.dbInstanceEndpointPort,
				DB_NAME: 'scrumpoker',
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
			// defaultRouteOptions: {
			// 	integration: new apigwv2Integrations.WebSocketLambdaIntegration('Default', handler),
			// },
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
