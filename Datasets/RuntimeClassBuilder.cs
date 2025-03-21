﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

//based on https://www.c-sharpcorner.com/uploadfile/87b416/dynamically-create-a-class-at-runtime/

namespace HlidacStatu.Datasets
{
    public class RuntimeClassBuilder
    {
        AssemblyName asemblyName;
        Dictionary<string, Type> properties = null;
        public RuntimeClassBuilder(Dictionary<string, Type> properties)
            : this("class_" + Guid.NewGuid().ToString("N"), properties)
        {
        }

        public Type GetPropertyType(string path)
        {
            if (properties.ContainsKey(path))
                return properties[path];
            else
                return null;
        }

        public string GetObjectId(object instance)
        {
            var id = GetPropertyValue(instance, "id");
            if (id == null)
                id = GetPropertyValue(instance, "Id");
            if (id == null)
                id = GetPropertyValue(instance, "ID");
            if (id == null)
                id = GetPropertyValue(instance, "iD");
            return (string)id;
        }
        public object GetPropertyValue(object instance, string propName)
        {
            if (instance == null)
                throw new ArgumentNullException("instance");
            if (string.IsNullOrEmpty(propName))
                throw new ArgumentNullException("propName");

            if (instance.GetType() != CreateType())
                throw new InvalidCastException($"Instance type {instance.GetType().FullName} different from {CreateType().FullName}");
            return CreateType().GetProperty(propName)?.GetValue(instance);

        }

        public void SetPropertyValue(object instance, string propName, object value)
        {
            if (instance == null)
                throw new ArgumentNullException("instance");
            if (string.IsNullOrEmpty(propName))
                throw new ArgumentNullException("propName");

            if (instance.GetType() != CreateType())
                throw new InvalidCastException($"Instance type {instance.GetType().FullName} different from {CreateType().FullName}");

            if (propName.Contains("."))
            {
                int found = propName.IndexOf('.');
                var rootPropName = propName.Substring(0, found);
                var subPropName = propName.Substring(found + 1);

                var subInstance = GetPropertyValue(instance, rootPropName);
                var rootProp = CreateType().GetProperty(rootPropName);
                if (rootProp != null && subInstance != null)
                {
                    var existingCustSubType = customSubTypes.FirstOrDefault(s => s.CreateType() == rootProp.PropertyType);
                    if (existingCustSubType != null)
                    {
                        existingCustSubType.SetPropertyValue(subInstance, subPropName, value);
                    }
                }

            }
            else
            {
                var propType = CreateType().GetProperty(propName)?.PropertyType;
                if (propType != null)
                    CreateType().GetProperty(propName)?.SetValue(instance, HlidacStatu.Util.ParseTools.ChangeType(value, propType));
            }
        }

        public RuntimeClassBuilder(string className, Dictionary<string, Type> properties)
        {
            if (string.IsNullOrEmpty(className))
                throw new ArgumentNullException("classNames");
            if (properties == null)
                throw new ArgumentNullException("properties");
            if (properties.Count == 0)
                throw new ArgumentException("No properties", "properties");

            asemblyName = new AssemblyName(className);
            this.properties = properties;
        }
        public object CreateObject() // string[] PropertyNames,Type[]Types)
        {
            var inst = Activator.CreateInstance(CreateType());
            foreach (var prop in inst.GetType().GetProperties())
            {
                Type t = prop.PropertyType;
                var existingCustSubType = customSubTypes.FirstOrDefault(s => s.CreateType() == t);
                if (existingCustSubType != null)
                {
                    SetPropertyValue(inst, prop.Name, existingCustSubType.CreateObject());
                }

            }
            return inst;
        }


        List<RuntimeClassBuilder> customSubTypes = new List<RuntimeClassBuilder>();

        Type _createdType = null;
        public Type CreateType() // string[] PropertyNames,Type[]Types)
        {
            if (_createdType == null)
            {

                TypeBuilder DynamicClass = CreateClass();
                CreateConstructor(DynamicClass);

                foreach (var kv in properties)
                {
                    if (kv.Value == typeof(object))
                    {
                        string prefix = kv.Key + ".";
                        Dictionary<string, Type> subProps = new Dictionary<string, Type>();
                        foreach (var sProp in properties)
                        {
                            if (sProp.Key.StartsWith(prefix)
                                && sProp.Key.IndexOf('.', prefix.Length) == -1 //no subObjects properties
                                )
                                subProps.Add(sProp.Key.Replace(prefix, ""), sProp.Value);
                        }

                        var subRcb = new RuntimeClassBuilder(subProps);
                        customSubTypes.Add(subRcb);
                        CreateProperty(DynamicClass, kv.Key, subRcb.CreateType());
                    }
                    else if (!kv.Key.Contains("."))
                        CreateProperty(DynamicClass, kv.Key, kv.Value);
                }

                _createdType = DynamicClass.CreateType();
            }
            return _createdType;
        }
        private TypeBuilder CreateClass()
        {
            var assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(asemblyName, AssemblyBuilderAccess.Run);
            ModuleBuilder moduleBuilder = assemblyBuilder.DefineDynamicModule("MainModule");
            TypeBuilder typeBuilder = moduleBuilder.DefineType(asemblyName.FullName
                                , TypeAttributes.Public |
                                TypeAttributes.Class |
                                TypeAttributes.AutoClass |
                                TypeAttributes.AnsiClass |
                                TypeAttributes.BeforeFieldInit |
                                TypeAttributes.AutoLayout
                                , null);
            return typeBuilder;
        }
        private void CreateConstructor(TypeBuilder typeBuilder)
        {
            typeBuilder.DefineDefaultConstructor(MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName);
        }
        private void CreateProperty(TypeBuilder typeBuilder, string propertyName, Type propertyType)
        {
            FieldBuilder fieldBuilder = typeBuilder.DefineField("_" + propertyName, propertyType, FieldAttributes.Private);

            PropertyBuilder propertyBuilder = typeBuilder.DefineProperty(propertyName, PropertyAttributes.HasDefault, propertyType, null);
            MethodBuilder getPropMthdBldr = typeBuilder.DefineMethod("get_" + propertyName, MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.HideBySig, propertyType, Type.EmptyTypes);
            ILGenerator getIl = getPropMthdBldr.GetILGenerator();

            getIl.Emit(OpCodes.Ldarg_0);
            getIl.Emit(OpCodes.Ldfld, fieldBuilder);
            getIl.Emit(OpCodes.Ret);

            MethodBuilder setPropMthdBldr = typeBuilder.DefineMethod("set_" + propertyName,
                  MethodAttributes.Public |
                  MethodAttributes.SpecialName |
                  MethodAttributes.HideBySig,
                  null, new[] { propertyType });

            ILGenerator setIl = setPropMthdBldr.GetILGenerator();
            Label modifyProperty = setIl.DefineLabel();
            Label exitSet = setIl.DefineLabel();

            setIl.MarkLabel(modifyProperty);
            setIl.Emit(OpCodes.Ldarg_0);
            setIl.Emit(OpCodes.Ldarg_1);
            setIl.Emit(OpCodes.Stfld, fieldBuilder);

            setIl.Emit(OpCodes.Nop);
            setIl.MarkLabel(exitSet);
            setIl.Emit(OpCodes.Ret);

            propertyBuilder.SetGetMethod(getPropMthdBldr);
            propertyBuilder.SetSetMethod(setPropMthdBldr);
        }
    }
}
