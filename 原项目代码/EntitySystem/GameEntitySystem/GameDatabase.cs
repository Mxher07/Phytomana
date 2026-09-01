using TemplatesDatabase;

namespace GameEntitySystem {
    public class GameDatabase {
        public Database Database { get; set; }

        public DatabaseObjectType FolderType { get; set; }

        public DatabaseObjectType ProjectTemplateType { get; set; }

        public DatabaseObjectType MemberSubsystemTemplateType { get; set; }

        public DatabaseObjectType SubsystemTemplateType { get; set; }

        public DatabaseObjectType EntityTemplateType { get; set; }

        public DatabaseObjectType MemberComponentTemplateType { get; set; }

        public DatabaseObjectType ComponentTemplateType { get; set; }

        public DatabaseObjectType ParameterSetType { get; set; }

        public DatabaseObjectType ParameterType { get; set; }

        public GameDatabase(Database database) {
            Database = database;
            FolderType = database.FindDatabaseObjectType("Folder", true);
            ProjectTemplateType = database.FindDatabaseObjectType("ProjectTemplate", true);
            MemberSubsystemTemplateType = database.FindDatabaseObjectType("MemberSubsystemTemplate", true);
            SubsystemTemplateType = database.FindDatabaseObjectType("SubsystemTemplate", true);
            EntityTemplateType = database.FindDatabaseObjectType("EntityTemplate", true);
            MemberComponentTemplateType = database.FindDatabaseObjectType("MemberComponentTemplate", true);
            ComponentTemplateType = database.FindDatabaseObjectType("ComponentTemplate", true);
            ParameterSetType = database.FindDatabaseObjectType("ParameterSet", true);
            ParameterType = database.FindDatabaseObjectType("Parameter", true);
        }
    }
}