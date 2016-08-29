using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Configuration;
using piSensorNet.Common.System;

[assembly: InternalsVisibleTo("piSensorNet.Tests")]

namespace piSensorNet.Common.Configuration
{
    public static class ReadOnlyConfiguration
    {
        private const string SettingsRootKey = "Settings";

        private static readonly ConcurrentDictionary<Type, Type> ConstructedTypes
            = new ConcurrentDictionary<Type, Type>();

        public static IpiSensorNetConfiguration Load(params string[] configFiles)
        {
            var builder = new ConfigurationBuilder()
                .AddJsonFile("globalConfig.json", true) // windows
                .AddJsonFile(@"..\..\..\..\..\globalConfig.json", true) // windows
                .AddJsonFile("../globalConfig.json", true); // linux

            foreach (var configFile in configFiles)
                builder.AddJsonFile(configFile);

            var configuration = builder.Build();

            return Proxify<IpiSensorNetConfiguration>(configuration);
        }

        internal static TConfiguration Proxify<TConfiguration>(IConfiguration source)
            where TConfiguration : class
        {
            var type = ConstructedTypes.GetOrAdd(Reflector.Instance<TConfiguration>.Type, ConstructType);
            var instance = Activator.CreateInstance(type, source);

            return (TConfiguration)instance;
        }

        private static Type ConstructType(Type configurationType)
        {
            // prepare names
            var typeName = $"Proxy_{configurationType.Name}";
            var assemblyName = $"piSensorNet.Common.Configuration.Dynamic_{configurationType.Name}";
            var moduleName = "Main";
            
            var typeBuilder = PrepareTypeBuilder(assemblyName, moduleName, typeName);
            
            typeBuilder.AddInterfaceImplementation(configurationType);

            var configurationField = DefineConfigruationProperty(typeBuilder, "Configuration");

            DefineIndexer(typeBuilder, configurationField);

            var propertyFields = DefineProperties(configurationType, typeBuilder);

            DefineConstructor(typeBuilder, configurationField, propertyFields);

            return typeBuilder.CreateType();
        }

        private static TypeBuilder PrepareTypeBuilder(string assemblyName, string moduleName, string typeName)
        {
            var assemblyBuilder = AppDomain.CurrentDomain.DefineDynamicAssembly(new AssemblyName(assemblyName), AssemblyBuilderAccess.Run);
            var moduleBuilder = assemblyBuilder.DefineDynamicModule(moduleName);
            var typeBuilder = moduleBuilder.DefineType(typeName,
                TypeAttributes.AutoLayout | TypeAttributes.Class | TypeAttributes.AnsiClass | TypeAttributes.BeforeFieldInit, // for internal class
                null);

            return typeBuilder;
        }

        private static Dictionary<PropertyInfo, FieldInfo> DefineProperties(Type configurationType, TypeBuilder typeBuilder)
        {
            var compilerGeneratedAttributeConstructor = Reflector.Instance<CompilerGeneratedAttribute>.Type.GetConstructor(new Type[0]);

            var properties = configurationType.GetProperties();
            var propertyFields = new Dictionary<PropertyInfo, FieldInfo>(properties.Length);

            foreach (var property in properties)
            {
                var propertyBackingField = DefineProperty(typeBuilder, property, compilerGeneratedAttributeConstructor);

                propertyFields.Add(property, propertyBackingField);
            }

            return propertyFields;
        }

        private static FieldBuilder DefineConfigruationProperty(TypeBuilder typeBuilder, string name)
        {
            // create private, readonly field for configuration
            var field = typeBuilder.DefineField("_" + name.ToLowerInvariant(),
                Reflector.Instance<IConfiguration>.Type,
                FieldAttributes.Private | FieldAttributes.InitOnly);

            var configurationProperty = typeBuilder.DefineProperty(name,
                PropertyAttributes.None,
                Reflector.Instance<IConfiguration>.Type,
                null);

            var configurationPropertyGetMethod = typeBuilder.DefineMethod("get_" + configurationProperty.Name,
                MethodAttributes.PrivateScope | MethodAttributes.Assembly | MethodAttributes.SpecialName | MethodAttributes.HideBySig,
                CallingConventions.Standard | CallingConventions.HasThis,
                configurationProperty.PropertyType,
                null);

            var configurationPropertyGetMethodCode = configurationPropertyGetMethod.GetILGenerator();

            configurationPropertyGetMethodCode.Emit(OpCodes.Ldarg_0);
            configurationPropertyGetMethodCode.Emit(OpCodes.Ldfld, field);
            configurationPropertyGetMethodCode.Emit(OpCodes.Ret);

            configurationProperty.SetGetMethod(configurationPropertyGetMethod);

            return field;
        }

        private static void DefineIndexer(TypeBuilder typeBuilder, FieldInfo configurationField)
        {
            var configurationGetItemMethod = Reflector.Instance<IConfiguration>.Indexer<string, string>((configuration, key) => configuration[key]);

            var getItemMethod = typeBuilder.DefineMethod(configurationGetItemMethod.Name,
                MethodAttributes.Public | MethodAttributes.Final | MethodAttributes.HideBySig | MethodAttributes.SpecialName | MethodAttributes.Virtual | MethodAttributes.NewSlot,
                CallingConventions.Standard | CallingConventions.HasThis,
                Reflector.Instance<String>.Type,
                new[] {Reflector.Instance<String>.Type});

            getItemMethod.DefineParameter(0, ParameterAttributes.None, configurationGetItemMethod.GetParameters()[0].Name);

            var getItemMethodCode = getItemMethod.GetILGenerator();

            {
                {
                    getItemMethodCode.Emit(OpCodes.Ldarg_0); // push this
                    getItemMethodCode.Emit(OpCodes.Ldfld, configurationField); // push configuration
                } // configuration field value

                getItemMethodCode.Emit(OpCodes.Ldarg_1); // push key

                getItemMethodCode.Emit(OpCodes.Callvirt, configurationGetItemMethod); // call get_Item from IConfiguration
            } // value from indexer

            getItemMethodCode.Emit(OpCodes.Ret);
        }

        private static FieldBuilder DefineProperty(TypeBuilder typeBuilder, PropertyInfo p, ConstructorInfo compilerGeneratedAttributeConstructor)
        {
            var property = typeBuilder.DefineProperty(p.Name,
                PropertyAttributes.None,
                CallingConventions.Standard | CallingConventions.HasThis,
                p.PropertyType,
                null);

            var propertyBackingField = typeBuilder.DefineField($"<{p.Name}>k__BackingField",
                p.PropertyType,
                FieldAttributes.Private | FieldAttributes.InitOnly);

            propertyBackingField.SetCustomAttribute(new CustomAttributeBuilder(compilerGeneratedAttributeConstructor, new object[0]));

            var propertyGetMethod = typeBuilder.DefineMethod("get_" + p.Name,
                MethodAttributes.PrivateScope | MethodAttributes.Public | MethodAttributes.Final | MethodAttributes.HideBySig | MethodAttributes.SpecialName | MethodAttributes.Virtual | MethodAttributes.NewSlot,
                CallingConventions.Standard | CallingConventions.HasThis,
                p.PropertyType,
                null);

            propertyGetMethod.SetCustomAttribute(new CustomAttributeBuilder(compilerGeneratedAttributeConstructor, new object[0]));

            var propertyGetMethodCode = propertyGetMethod.GetILGenerator();

            {
                {
                    propertyGetMethodCode.Emit(OpCodes.Ldarg_0);

                    propertyGetMethodCode.Emit(OpCodes.Ldfld, propertyBackingField);
                } // property backing field value

                propertyGetMethodCode.Emit(OpCodes.Ret);
            }

            property.SetGetMethod(propertyGetMethod);

            return propertyBackingField;
        }

        private static void DefineConstructor(TypeBuilder typeBuilder, FieldInfo configurationField, IReadOnlyDictionary<PropertyInfo, FieldInfo> propertyFields)
        {
            var constructor = typeBuilder.DefineConstructor(
                MethodAttributes.PrivateScope | MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName,
                CallingConventions.Standard | CallingConventions.HasThis,
                new[] {Reflector.Instance<IConfiguration>.Type});

            constructor.DefineParameter(0, ParameterAttributes.None, "configuration");

            var constructorCode = constructor.GetILGenerator();

            {
                constructorCode.Emit(OpCodes.Ldarg_0); // push this for Stfld
                constructorCode.Emit(OpCodes.Ldarg_1); // push configuration for Stfld

                constructorCode.Emit(OpCodes.Stfld, configurationField); // store field _configuration
            }

            constructorCode.Emit(OpCodes.Nop);

            var stringFormat3Method = Reflector.Static.Method<string, object, object, string>(() => String.Format);
            var configurationGetItemMethod = Reflector.Instance<IConfiguration>.Indexer<string, string>((configuration, key) => configuration[key]);

            foreach (var p in propertyFields)
            {
                constructorCode.Emit(OpCodes.Ldarg_0); // push this for Stfld
                {
                    {
                        constructorCode.Emit(OpCodes.Ldarg_0); // push this for callvirt
                        constructorCode.Emit(OpCodes.Ldfld, configurationField); // load configuration
                        {
                            constructorCode.Emit(OpCodes.Ldstr, "{0}:{1}"); // load format for property name
                            constructorCode.Emit(OpCodes.Ldstr, SettingsRootKey); // load arg 0 for String.Format
                            constructorCode.Emit(OpCodes.Ldstr, p.Key.Name); // load arg 1 for String.Format

                            constructorCode.Emit(OpCodes.Call, stringFormat3Method); // call String.Format, push result (key) for callvirt
                        } // formatted key

                        constructorCode.Emit(OpCodes.Callvirt, configurationGetItemMethod);
                    } // value from indexer

                    HandlePropertyConversion(constructorCode, p.Key);
                } // [converted] value

                constructorCode.Emit(OpCodes.Stfld, p.Value);

                constructorCode.Emit(OpCodes.Nop);
            }

            constructorCode.Emit(OpCodes.Ret);
        }

        private static void HandlePropertyConversion(ILGenerator constructorCode, PropertyInfo property)
        {
            if (property.PropertyType == Reflector.Instance<String>.Type)
                return;

            if (property.PropertyType == Reflector.Instance<Char>.Type)
                constructorCode.Emit(OpCodes.Call, Reflector.Static.Method<IEnumerable<char>, char>(() => Enumerable.Single));
            else if (property.PropertyType == Reflector.Instance<Int32>.Type)
                constructorCode.Emit(OpCodes.Call, Reflector.Static.Method<string, int>(() => int.Parse));
        }
    }
}