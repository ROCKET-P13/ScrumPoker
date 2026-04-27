import * as cdk from 'aws-cdk-lib';
import { Construct } from 'constructs';
import * as ec2 from 'aws-cdk-lib/aws-ec2';
import * as rds from 'aws-cdk-lib/aws-rds';
import * as lambda from 'aws-cdk-lib/aws-lambda';
import * as secretsmanager from 'aws-cdk-lib/aws-secretsmanager';
import * as apigw from 'aws-cdk-lib/aws-apigatewayv2';
import * as integrations from 'aws-cdk-lib/aws-apigatewayv2-integrations';
import * as path from 'path';

export class ScrumPokerInfrastructureStack extends cdk.Stack {
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

		const dbSecret = new secretsmanager.Secret(this, 'DbSecret', {
			secretName: 'ScrumPokerAPI_Secret',
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
			credentials: rds.Credentials.fromSecret(dbSecret),
			vpcSubnets: { subnetType: ec2.SubnetType.PRIVATE_WITH_EGRESS },
			securityGroups: [dbSecurityGroup],
			allocatedStorage: 20,
			maxAllocatedStorage: 100,
			publiclyAccessible: false,
			databaseName: 'books',
			removalPolicy: cdk.RemovalPolicy.DESTROY,
		});

		const apiLambda = new lambda.Function(this, 'ApiLambda', {
			runtime: lambda.Runtime.DOTNET_8,
			handler: 'ScrumPokerAPI::ScrumPokerAPI.LambdaEntryPoint::FunctionHandlerAsync',
			code: lambda.Code.fromAsset(
				path.join(__dirname, '../../ScrumPokerAPI/bin/Release/net8.0/publish')
			),
			vpc,
			timeout: cdk.Duration.seconds(30),
			memorySize: 512,
			securityGroups: [lambdaSecurityGroup],
			vpcSubnets: { subnetType: ec2.SubnetType.PRIVATE_WITH_EGRESS },
			environment: {
				POSTGRES_SECRET_ARN: dbSecret.secretArn,
				DB_HOST: dbInstance.dbInstanceEndpointAddress,
				DB_PORT: dbInstance.dbInstanceEndpointPort,
				DB_NAME: 'scrumpoker',
			},
		});

		dbSecret.grantRead(apiLambda);

		const httpApi = new apigw.HttpApi(this, 'ApiGateway', {
			apiName: 'ScrumPokerAPI',
			description: 'HTTP API for ASP.NET Core on Lambda',
		});

		httpApi.addRoutes({
			path: '/{proxy+}',
			methods: [apigw.HttpMethod.ANY],
			integration: new integrations.HttpLambdaIntegration('ApiLambdaIntegration', apiLambda),
		});

		new cdk.CfnOutput(this, 'ApiUrl', { value: httpApi.url! });
		new cdk.CfnOutput(this, 'DbEndpoint', { value: dbInstance.dbInstanceEndpointAddress });
	}
}
