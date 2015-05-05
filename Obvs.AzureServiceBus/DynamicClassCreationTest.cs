using Microsoft.ServiceBus.Messaging;
using System;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading.Tasks;

namespace Test
{
	internal interface IBrokeredMessageWrapper
	{
		BrokeredMessage BrokeredMessage
		{
			get;
			set;
		}
	}
	
	internal interface IBrokeredMessageWrapperProvider
	{
		IBrokeredMessageWrapper Create<TServiceMessage>();
	}
	
	internal class BrokeredMessageWrapperProvider : IBrokeredMessageWrapperProvider
	{
		public IBrokeredMessageWrapper Create<TServiceMessage>()
		{
			return GetMessageWrapperFactory<TServiceMessage>()();
		}

		private static Func<IBrokeredMessageWrapper> GetMessageWrapperFactory<TServiceMessage>()
		{
			return Expression.Lambda<Func<IBrokeredMessageWrapper>>(Expression.New(CreateMessageWrapperType<TServiceMessage>())).Compile();
		}

		private static Type CreateMessageWrapperType<TServiceMessage>()
		{
			Type serviceMessageType = typeof(TServiceMessage);

			AssemblyName assemblyName = new AssemblyName("");
			AssemblyBuilder assemblyBuilder = AppDomain.CurrentDomain.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run);
			ModuleBuilder moduleBuilder = assemblyBuilder.DefineDynamicModule("DefaultModule");
			TypeBuilder typeBuilder = moduleBuilder.DefineType("Whatever", TypeAttributes.AutoClass | TypeAttributes.AutoLayout | TypeAttributes.Sealed | TypeAttributes.Public, serviceMessageType);
			FieldBuilder brokeredMessageFieldBuilder = typeBuilder.DefineField("_brokeredMessage", typeof(BrokeredMessage), FieldAttributes.Private);
			typeBuilder.AddInterfaceImplementation(typeof(IBrokeredMessageWrapper));

			PropertyBuilder brokeredMessagePropertyBuilder = typeBuilder.DefineProperty("BrokeredMessage", PropertyAttributes.None, typeof(BrokeredMessage), Type.EmptyTypes);

			MethodBuilder brokeredMessagePropertySetterMethodBuilder = typeBuilder.DefineMethod("set_BrokeredMessage", MethodAttributes.HideBySig | MethodAttributes.Public);

			ILGenerator generator = brokeredMessagePropertySetterMethodBuilder.GetILGenerator();
			generator.Emit(OpCodes.Ldarg_0);
			generator.Emit(OpCodes.Stfld, brokeredMessageFieldBuilder);
			generator.Emit(OpCodes.Ret);

			typeBuilder.DefineMethodOverride(brokeredMessagePropertySetterMethodBuilder, typeof(IBrokeredMessageWrapper).GetProperty("BrokeredMessage").GetSetMethod());

			MethodBuilder brokeredMessagePropertyGetterMethodBuilder = typeBuilder.DefineMethod("get_BrokeredMessage", MethodAttributes.HideBySig | MethodAttributes.Public, typeof(BrokeredMessage), Type.EmptyTypes);

			generator = brokeredMessagePropertyGetterMethodBuilder.GetILGenerator();
			generator.Emit(OpCodes.Ldarg_0);
			generator.Emit(OpCodes.Ldfld, brokeredMessageFieldBuilder);
			generator.Emit(OpCodes.Ret);

			typeBuilder.DefineMethodOverride(brokeredMessagePropertyGetterMethodBuilder, typeof(IBrokeredMessageWrapper).GetProperty("BrokeredMessage").GetGetMethod());
			BuildMessageControlAsyncMethod("CompleteAsync", typeBuilder, brokeredMessageFieldBuilder);
			BuildMessageControlAsyncMethod("AbandonAsync", typeBuilder, brokeredMessageFieldBuilder);
			BuildMessageControlAsyncMethod("RenewAsync", typeBuilder, brokeredMessageFieldBuilder);
			BuildMessageControlAsyncMethod("RejectAsync", typeBuilder, brokeredMessageFieldBuilder, new[] { typeof(string), typeof(string) });

			return typeBuilder.CreateType();
		}

		private static void BuildMessageControlAsyncMethod(string messageControlMethodName, TypeBuilder typeBuilder, FieldBuilder brokeredMessageFieldBuilder, Type[] parameters = null)
		{
			MethodBuilder completeAsyncMethodBuilder = typeBuilder.DefineMethod(messageControlMethodName, MethodAttributes.Public | MethodAttributes.HideBySig, typeof(Task), Type.EmptyTypes);
			ILGenerator generator = completeAsyncMethodBuilder.GetILGenerator();

			generator.Emit(OpCodes.Ldarg_0);
			generator.Emit(OpCodes.Ldfld, brokeredMessageFieldBuilder);

			if(parameters != null)
			{
				for(int parameterIndex = 1; parameterIndex < parameters.Length; parameterIndex++)
				{
					generator.Emit(OpCodes.Ldarg, parameterIndex);
				}
			}

			generator.Emit(OpCodes.Callvirt, typeof(BrokeredMessage).GetMethod(messageControlMethodName));
			generator.Emit(OpCodes.Ret);

			typeBuilder.DefineMethodOverride(completeAsyncMethodBuilder, typeof(IBrokeredMessageWrapper).GetMethod(messageControlMethodName));
		}
	}
}