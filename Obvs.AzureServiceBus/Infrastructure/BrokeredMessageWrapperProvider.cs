using System;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading.Tasks;
using Microsoft.ServiceBus.Messaging;

namespace Obvs.AzureServiceBus.Infrastructure
{
    internal interface IBrokeredMessageBasedMessage
    {
        BrokeredMessage BrokeredMessage
        {
            get;
            set;
        }
    }

    internal interface IBrokeredMessageBasedMessageTypeBuilder
    {
        Type CreateBrokeredMessageBasedMessageSubclass<TServiceMessage>();
    }

    internal sealed class BrokeredMessageBasedMessageSubclassBuilder : IBrokeredMessageBasedMessageTypeBuilder
    {
        public Type CreateBrokeredMessageBasedMessageSubclass<TServiceMessage>()
        {
            Type serviceMessageType = typeof(TServiceMessage);

            AssemblyName assemblyName = new AssemblyName("Obvs.AzureServiceBus.DynamicPeekLockMessageTypes");
            
            AssemblyBuilder assemblyBuilder = AppDomain.CurrentDomain.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run);
            
            ModuleBuilder moduleBuilder = assemblyBuilder.DefineDynamicModule("DefaultModule");
            
            TypeBuilder typeBuilder = moduleBuilder.DefineType("Whatever", TypeAttributes.AutoClass | TypeAttributes.AutoLayout | TypeAttributes.Sealed | TypeAttributes.Public, serviceMessageType);
            
            FieldBuilder brokeredMessageFieldBuilder = typeBuilder.DefineField("_brokeredMessage", typeof(BrokeredMessage), FieldAttributes.Private);
            typeBuilder.AddInterfaceImplementation(typeof(IBrokeredMessageBasedMessage));

            BuildBrokeredMessageProperty(typeBuilder, brokeredMessageFieldBuilder);

            return typeBuilder.CreateType();
        }

        private static void BuildBrokeredMessageProperty(TypeBuilder typeBuilder, FieldBuilder brokeredMessageFieldBuilder)
        {
            PropertyBuilder brokeredMessagePropertyBuilder = typeBuilder.DefineProperty("BrokeredMessage", PropertyAttributes.None, typeof(BrokeredMessage), Type.EmptyTypes);

            MethodBuilder brokeredMessagePropertySetterMethodBuilder = typeBuilder.DefineMethod("set_BrokeredMessage", MethodAttributes.HideBySig | MethodAttributes.Public);

            ILGenerator generator = brokeredMessagePropertySetterMethodBuilder.GetILGenerator();
            generator.Emit(OpCodes.Ldarg_0);
            generator.Emit(OpCodes.Stfld, brokeredMessageFieldBuilder);
            generator.Emit(OpCodes.Ret);

            typeBuilder.DefineMethodOverride(brokeredMessagePropertySetterMethodBuilder, typeof(IBrokeredMessageBasedMessage).GetProperty("BrokeredMessage").GetSetMethod());

            MethodBuilder brokeredMessagePropertyGetterMethodBuilder = typeBuilder.DefineMethod("get_BrokeredMessage", MethodAttributes.HideBySig | MethodAttributes.Public, typeof(BrokeredMessage), Type.EmptyTypes);

            generator = brokeredMessagePropertyGetterMethodBuilder.GetILGenerator();
            generator.Emit(OpCodes.Ldarg_0);
            generator.Emit(OpCodes.Ldfld, brokeredMessageFieldBuilder);
            generator.Emit(OpCodes.Ret);

            typeBuilder.DefineMethodOverride(brokeredMessagePropertyGetterMethodBuilder, typeof(IBrokeredMessageBasedMessage).GetProperty("BrokeredMessage").GetGetMethod());
        }
    }
}
