﻿//------------------------------------------------------------------------------
// <auto-generated>
//     Этот код создан программой.
//     Исполняемая версия:4.0.30319.18052
//
//     Изменения в этом файле могут привести к неправильной работе и будут потеряны в случае
//     повторной генерации кода.
// </auto-generated>
//------------------------------------------------------------------------------

namespace TcpServer.Core.Mintrans {
    using System;
    
    
    /// <summary>
    ///   Класс ресурса со строгой типизацией для поиска локализованных строк и т.д.
    /// </summary>
    // Этот класс создан автоматически классом StronglyTypedResourceBuilder
    // с помощью такого средства, как ResGen или Visual Studio.
    // Чтобы добавить или удалить член, измените файл .ResX и снова запустите ResGen
    // с параметром /str или перестройте свой проект VS.
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("System.Resources.Tools.StronglyTypedResourceBuilder", "4.0.0.0")]
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
    [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
    internal class SoapTemplates {
        
        private static global::System.Resources.ResourceManager resourceMan;
        
        private static global::System.Globalization.CultureInfo resourceCulture;
        
        [global::System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        internal SoapTemplates() {
        }
        
        /// <summary>
        ///   Возвращает кэшированный экземпляр ResourceManager, использованный этим классом.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static global::System.Resources.ResourceManager ResourceManager {
            get {
                if (object.ReferenceEquals(resourceMan, null)) {
                    global::System.Resources.ResourceManager temp = new global::System.Resources.ResourceManager("TcpServer.Core.Mintrans.SoapTemplates", typeof(SoapTemplates).Assembly);
                    resourceMan = temp;
                }
                return resourceMan;
            }
        }
        
        /// <summary>
        ///   Перезаписывает свойство CurrentUICulture текущего потока для всех
        ///   обращений к ресурсу с помощью этого класса ресурса со строгой типизацией.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static global::System.Globalization.CultureInfo Culture {
            get {
                return resourceCulture;
            }
            set {
                resourceCulture = value;
            }
        }
        
        /// <summary>
        ///   Ищет локализованную строку, похожую на &lt;?xml version=&quot;1.0&quot; encoding=&quot;windows-1251&quot; ?&gt;
        ///&lt;soapenv:Envelope xmlns:env=&quot;http://schemas.xmlsoap.org/soap/envelope&quot;&gt;
        ///  &lt;soapenv:Header /&gt;
        ///  &lt;soapenv:Body&gt;
        ///    &lt;ws:PutCoord&gt;
        ///      &lt;ObjectID&gt;{0}&lt;/ObjectID&gt;
        ///      &lt;Coord time=&quot;{1}&quot; lon=&quot;{2}&quot; lat=&quot;{3}&quot; alt=&quot;{4}&quot; speed=&quot;{5}&quot; dir=&quot;{6}&quot; valid=&quot;{7}&quot; /&gt;
        ///      &lt;DigI inpnum=&quot;2&quot; /&gt;
        ///    &lt;/ws:PutCoord&gt;
        ///  &lt;/soapenv:Body&gt;
        ///&lt;/soapenv:Envelope&gt;.
        /// </summary>
        internal static string Alarm {
            get {
                return ResourceManager.GetString("Alarm", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Ищет локализованную строку, похожую на &lt;?xml version=&quot;1.0&quot; encoding=&quot;windows-1251&quot; ?&gt;
        ///&lt;soapenv:Envelope xmlns:env=&quot;http://schemas.xmlsoap.org/soap/envelope&quot;&gt;
        ///  &lt;soapenv:Header /&gt;
        ///  &lt;soapenv:Body&gt;
        ///    &lt;ws:PutCoord&gt;
        ///      &lt;ObjectID&gt;{0}&lt;/ObjectID&gt;
        ///      &lt;Coord time=&quot;{1}&quot; lon=&quot;{2}&quot; lat=&quot;{3}&quot; alt=&quot;{4}&quot; speed=&quot;{5}&quot; dir=&quot;{6}&quot; valid=&quot;{7}&quot; /&gt;
        ///    &lt;/ws:PutCoord&gt;
        ///  &lt;/soapenv:Body&gt;
        ///&lt;/soapenv:Envelope&gt;.
        /// </summary>
        internal static string LocationAndState {
            get {
                return ResourceManager.GetString("LocationAndState", resourceCulture);
            }
        }
    }
}
