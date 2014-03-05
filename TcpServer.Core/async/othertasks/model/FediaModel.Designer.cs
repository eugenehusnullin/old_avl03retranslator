﻿//------------------------------------------------------------------------------
// <auto-generated>
//    Этот код был создан из шаблона.
//
//    Изменения, вносимые в этот файл вручную, могут привести к непредвиденной работе приложения.
//    Изменения, вносимые в этот файл вручную, будут перезаписаны при повторном создании кода.
// </auto-generated>
//------------------------------------------------------------------------------

using System;
using System.ComponentModel;
using System.Data.EntityClient;
using System.Data.Objects;
using System.Data.Objects.DataClasses;
using System.Linq;
using System.Runtime.Serialization;
using System.Xml.Serialization;

[assembly: EdmSchemaAttribute()]
namespace TcpServer.Core.async.othertasks.model
{
    #region Контексты
    
    /// <summary>
    /// Нет доступной документации по метаданным.
    /// </summary>
    public partial class FediaEntities : ObjectContext
    {
        #region Конструкторы
    
        /// <summary>
        /// Инициализирует новый объект FediaEntities, используя строку соединения из раздела "FediaEntities" файла конфигурации приложения.
        /// </summary>
        public FediaEntities() : base("name=FediaEntities", "FediaEntities")
        {
            this.ContextOptions.LazyLoadingEnabled = true;
            OnContextCreated();
        }
    
        /// <summary>
        /// Инициализация нового объекта FediaEntities.
        /// </summary>
        public FediaEntities(string connectionString) : base(connectionString, "FediaEntities")
        {
            this.ContextOptions.LazyLoadingEnabled = true;
            OnContextCreated();
        }
    
        /// <summary>
        /// Инициализация нового объекта FediaEntities.
        /// </summary>
        public FediaEntities(EntityConnection connection) : base(connection, "FediaEntities")
        {
            this.ContextOptions.LazyLoadingEnabled = true;
            OnContextCreated();
        }
    
        #endregion
    
        #region Разделяемые методы
    
        partial void OnContextCreated();
    
        #endregion
    
        #region Свойства ObjectSet
    
        /// <summary>
        /// Нет доступной документации по метаданным.
        /// </summary>
        public ObjectSet<maindata> maindatas
        {
            get
            {
                if ((_maindatas == null))
                {
                    _maindatas = base.CreateObjectSet<maindata>("maindatas");
                }
                return _maindatas;
            }
        }
        private ObjectSet<maindata> _maindatas;

        #endregion

        #region Методы AddTo
    
        /// <summary>
        /// Устаревший метод для добавления новых объектов в набор EntitySet maindatas. Взамен можно использовать метод .Add связанного свойства ObjectSet&lt;T&gt;.
        /// </summary>
        public void AddTomaindatas(maindata maindata)
        {
            base.AddObject("maindatas", maindata);
        }

        #endregion

    }

    #endregion

    #region Сущности
    
    /// <summary>
    /// Нет доступной документации по метаданным.
    /// </summary>
    [EdmEntityTypeAttribute(NamespaceName="FediaEntities1", Name="maindata")]
    [Serializable()]
    [DataContractAttribute(IsReference=true)]
    public partial class maindata : EntityObject
    {
        #region Фабричный метод
    
        /// <summary>
        /// Создание нового объекта maindata.
        /// </summary>
        /// <param name="id">Исходное значение свойства id.</param>
        /// <param name="text">Исходное значение свойства text.</param>
        /// <param name="date">Исходное значение свойства date.</param>
        /// <param name="imei">Исходное значение свойства imei.</param>
        /// <param name="lat">Исходное значение свойства lat.</param>
        /// <param name="lon">Исходное значение свойства lon.</param>
        /// <param name="speed">Исходное значение свойства speed.</param>
        /// <param name="gpsdate">Исходное значение свойства gpsdate.</param>
        public static maindata Createmaindata(global::System.Int32 id, global::System.String text, global::System.DateTime date, global::System.String imei, global::System.String lat, global::System.String lon, global::System.String speed, global::System.String gpsdate)
        {
            maindata maindata = new maindata();
            maindata.id = id;
            maindata.text = text;
            maindata.date = date;
            maindata.imei = imei;
            maindata.lat = lat;
            maindata.lon = lon;
            maindata.speed = speed;
            maindata.gpsdate = gpsdate;
            return maindata;
        }

        #endregion

        #region Простые свойства
    
        /// <summary>
        /// Нет доступной документации по метаданным.
        /// </summary>
        [EdmScalarPropertyAttribute(EntityKeyProperty=true, IsNullable=false)]
        [DataMemberAttribute()]
        public global::System.Int32 id
        {
            get
            {
                return _id;
            }
            set
            {
                if (_id != value)
                {
                    OnidChanging(value);
                    ReportPropertyChanging("id");
                    _id = StructuralObject.SetValidValue(value, "id");
                    ReportPropertyChanged("id");
                    OnidChanged();
                }
            }
        }
        private global::System.Int32 _id;
        partial void OnidChanging(global::System.Int32 value);
        partial void OnidChanged();
    
        /// <summary>
        /// Нет доступной документации по метаданным.
        /// </summary>
        [EdmScalarPropertyAttribute(EntityKeyProperty=false, IsNullable=false)]
        [DataMemberAttribute()]
        public global::System.String text
        {
            get
            {
                return _text;
            }
            set
            {
                OntextChanging(value);
                ReportPropertyChanging("text");
                _text = StructuralObject.SetValidValue(value, false, "text");
                ReportPropertyChanged("text");
                OntextChanged();
            }
        }
        private global::System.String _text;
        partial void OntextChanging(global::System.String value);
        partial void OntextChanged();
    
        /// <summary>
        /// Нет доступной документации по метаданным.
        /// </summary>
        [EdmScalarPropertyAttribute(EntityKeyProperty=false, IsNullable=false)]
        [DataMemberAttribute()]
        public global::System.DateTime date
        {
            get
            {
                return _date;
            }
            set
            {
                OndateChanging(value);
                ReportPropertyChanging("date");
                _date = StructuralObject.SetValidValue(value, "date");
                ReportPropertyChanged("date");
                OndateChanged();
            }
        }
        private global::System.DateTime _date;
        partial void OndateChanging(global::System.DateTime value);
        partial void OndateChanged();
    
        /// <summary>
        /// Нет доступной документации по метаданным.
        /// </summary>
        [EdmScalarPropertyAttribute(EntityKeyProperty=false, IsNullable=false)]
        [DataMemberAttribute()]
        public global::System.String imei
        {
            get
            {
                return _imei;
            }
            set
            {
                OnimeiChanging(value);
                ReportPropertyChanging("imei");
                _imei = StructuralObject.SetValidValue(value, false, "imei");
                ReportPropertyChanged("imei");
                OnimeiChanged();
            }
        }
        private global::System.String _imei;
        partial void OnimeiChanging(global::System.String value);
        partial void OnimeiChanged();
    
        /// <summary>
        /// Нет доступной документации по метаданным.
        /// </summary>
        [EdmScalarPropertyAttribute(EntityKeyProperty=false, IsNullable=false)]
        [DataMemberAttribute()]
        public global::System.String lat
        {
            get
            {
                return _lat;
            }
            set
            {
                OnlatChanging(value);
                ReportPropertyChanging("lat");
                _lat = StructuralObject.SetValidValue(value, false, "lat");
                ReportPropertyChanged("lat");
                OnlatChanged();
            }
        }
        private global::System.String _lat;
        partial void OnlatChanging(global::System.String value);
        partial void OnlatChanged();
    
        /// <summary>
        /// Нет доступной документации по метаданным.
        /// </summary>
        [EdmScalarPropertyAttribute(EntityKeyProperty=false, IsNullable=false)]
        [DataMemberAttribute()]
        public global::System.String lon
        {
            get
            {
                return _lon;
            }
            set
            {
                OnlonChanging(value);
                ReportPropertyChanging("lon");
                _lon = StructuralObject.SetValidValue(value, false, "lon");
                ReportPropertyChanged("lon");
                OnlonChanged();
            }
        }
        private global::System.String _lon;
        partial void OnlonChanging(global::System.String value);
        partial void OnlonChanged();
    
        /// <summary>
        /// Нет доступной документации по метаданным.
        /// </summary>
        [EdmScalarPropertyAttribute(EntityKeyProperty=false, IsNullable=false)]
        [DataMemberAttribute()]
        public global::System.String speed
        {
            get
            {
                return _speed;
            }
            set
            {
                OnspeedChanging(value);
                ReportPropertyChanging("speed");
                _speed = StructuralObject.SetValidValue(value, false, "speed");
                ReportPropertyChanged("speed");
                OnspeedChanged();
            }
        }
        private global::System.String _speed;
        partial void OnspeedChanging(global::System.String value);
        partial void OnspeedChanged();
    
        /// <summary>
        /// Нет доступной документации по метаданным.
        /// </summary>
        [EdmScalarPropertyAttribute(EntityKeyProperty=false, IsNullable=false)]
        [DataMemberAttribute()]
        public global::System.String gpsdate
        {
            get
            {
                return _gpsdate;
            }
            set
            {
                OngpsdateChanging(value);
                ReportPropertyChanging("gpsdate");
                _gpsdate = StructuralObject.SetValidValue(value, false, "gpsdate");
                ReportPropertyChanged("gpsdate");
                OngpsdateChanged();
            }
        }
        private global::System.String _gpsdate;
        partial void OngpsdateChanging(global::System.String value);
        partial void OngpsdateChanged();

        #endregion

    }

    #endregion

}
