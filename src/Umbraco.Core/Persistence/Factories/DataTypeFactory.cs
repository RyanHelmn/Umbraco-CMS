﻿using System;
using System.Globalization;
using Newtonsoft.Json;
using Umbraco.Core.Models;
using Umbraco.Core.Persistence.Dtos;
using Umbraco.Core.PropertyEditors;

namespace Umbraco.Core.Persistence.Factories
{
    internal static class DataTypeFactory
    {
        public static IDataType BuildEntity(DataTypeDto dto, PropertyEditor editor)
        {
            var dataType = new DataType(dto.EditorAlias);

            try
            {
                dataType.DisableChangeTracking();

                dataType.CreateDate = dto.NodeDto.CreateDate;
                dataType.DatabaseType = dto.DbType.EnumParse<ValueStorageType>(true);
                dataType.Id = dto.NodeId;
                dataType.Key = dto.NodeDto.UniqueId;
                dataType.Level = dto.NodeDto.Level;
                dataType.UpdateDate = dto.NodeDto.CreateDate;
                dataType.Name = dto.NodeDto.Text;
                dataType.ParentId = dto.NodeDto.ParentId;
                dataType.Path = dto.NodeDto.Path;
                dataType.SortOrder = dto.NodeDto.SortOrder;
                dataType.Trashed = dto.NodeDto.Trashed;
                dataType.CreatorId = dto.NodeDto.UserId ?? 0;

                dataType.EditorAlias = editor.Alias;
                dataType.SetConfiguration(dto.Configuration, editor);

                // reset dirty initial properties (U4-1946)
                dataType.ResetDirtyProperties(false);
                return dataType;
            }
            finally
            {
                dataType.EnableChangeTracking();
            }
        }

        public static DataTypeDto BuildDto(IDataType entity)
        {
            var dataTypeDto = new DataTypeDto
            {
                EditorAlias = entity.EditorAlias,
                NodeId = entity.Id,
                DbType = entity.DatabaseType.ToString(),
                Configuration = JsonConvert.SerializeObject(entity.Configuration),
                NodeDto = BuildNodeDto(entity)
            };

            return dataTypeDto;
        }

        private static NodeDto BuildNodeDto(IDataType entity)
        {
            var nodeDto = new NodeDto
            {
                CreateDate = entity.CreateDate,
                NodeId = entity.Id,
                Level = Convert.ToInt16(entity.Level),
                NodeObjectType = Constants.ObjectTypes.DataType,
                ParentId = entity.ParentId,
                Path = entity.Path,
                SortOrder = entity.SortOrder,
                Text = entity.Name,
                Trashed = entity.Trashed,
                UniqueId = entity.Key,
                UserId = entity.CreatorId
            };

            return nodeDto;
        }
    }
}
