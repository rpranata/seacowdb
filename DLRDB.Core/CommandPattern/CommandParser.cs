using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DLRDB.Core.DataStructure;
using System.Text.RegularExpressions;

namespace DLRDB.Core.CommandPattern
{
    public static class CommandParser
    {
        /// <summary>
        /// We assume that the first 6 digits is already omitted (SELECT,INSERT,UPDATE,DELETE)
        /// This is the supported format : 
        /// 
        /// - SELECT *
        ///   (i.e. Select all rows )
        ///   
        /// - SELECT 1-7 
        ///   (i.e. Select Row 1 -> 7)
        ///   
        /// Notes : 
        /// - All criteria are assumed to be and AND operator, OR operator is not supported
        /// -
        /// </summary>
        /// <param name="expression"></param>
        /// <returns></returns>
        public static SelectCommand ParseSelectCommand(ITable table,String expression)
        {
            List<String> listSelected = new List<String>();
            List<Field> listCriteria = new List<Field>();
            SelectCommand tempCommand = null;


            expression = expression.Trim();

            if (expression.StartsWith("*"))
            {
                tempCommand = new SelectCommand(table,0,table.RowCount()-1);
            }
            else
            {
                String[] arrSplitExpression = Regex.Split(expression, "-");

                Int32 startIndex = Convert.ToInt32(arrSplitExpression[0])-1;
                Int32 endIndex = Convert.ToInt32(arrSplitExpression[1])-1;

                tempCommand = new SelectCommand(table,startIndex, endIndex);

            }

            return tempCommand;

        }


        /// <summary>
        /// We assume that the first 6 digits is already omitted (SELECT,INSERT,UPDATE,DELETE)
        /// This is the supported format : 
        /// SELECT col1,col2 FROM MyTable WHERE col3=2009,col4=john
        /// 
        /// Notes : 
        /// - All criteria are assumed to be and AND operator, OR operator is not supported
        /// -
        /// </summary>
        /// <param name="expression"></param>
        /// <returns></returns>
        //public static SelectCommand ParseSelectCommand_Obsolete(String expression)
        //{
        //    List<String> listSelected = new List<String>();
        //    List<Field> listCriteria = new List<Field>();

        //    String[] arrSplitExpression = Regex.Split(expression, "FROM");

        //    String selectedFieldNamePieces = arrSplitExpression[0];
        //    String tableNameAndCriteriaPieces = arrSplitExpression[1];

        //    String[] arrSplitTableNameAndCriteria = Regex.Split(arrSplitExpression[1], "WHERE");
        //    String tableName = arrSplitTableNameAndCriteria[0];
        //    String criteriaPieces = arrSplitTableNameAndCriteria[1];

        //    String [] arrCriteria = criteriaPieces.Split(',');
        //    foreach (String tempCriteria in arrCriteria)
        //    {
        //        String[] arrCriteriaKeyValuePair = tempCriteria.Trim().Split('=');

        //        Field tempField = null;

        //        int tempInt;
        //        if (Int32.TryParse(arrCriteriaKeyValuePair[1],out tempInt))
        //        {
        //            tempField = new Int32Field(arrCriteriaKeyValuePair[0]);
        //            tempField.setValue(Convert.ToInt32(arrCriteriaKeyValuePair[1]));
        //        }
        //        else
        //        {
        //            tempField = new StringField(arrCriteriaKeyValuePair[0]);
        //            tempField.setValue(arrCriteriaKeyValuePair[1]);
        //        }

        //        listCriteria.Add (tempField);
        //    }

        //    String[] arrFieldName = selectedFieldNamePieces.Replace(" ","").Split(',');
        //    listSelected.AddRange(arrFieldName);
            
        //    SelectCommand tempCommand = new SelectCommand(listSelected,listCriteria);
            
        //    return tempCommand;
        //}
     
    }
}
