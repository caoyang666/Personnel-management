using HEBTU.Web.BLL;
using HEBTU.Web.Models;
using HelperLibrary.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using HEBTU.Common;
using HEBTU.Web.Model;
using HEBTU.Db;
using System.Data;
using Newtonsoft.Json;
using System.Data.SqlClient;
using System.Data.Entity.Validation;

namespace HEBTU.Web.Controllers
{
    public class AcademicCommunicationController: Controller
    {
        //
        // GET: /AcademicCommunication/

        public ActionResult Index()
        {
            return View();
        }

        #region 学术交流

        public ActionResult XSJL_List()
        {
            return View();
        }
        /// <summary>
        /// 获取学术交流列表
        /// </summary>
        /// <returns></returns>
        public string GetXSJL_List()
        {
            int pagenum = RequestHelper.GetInt("pagenum", 0);//获取当前页
            int pagesize = RequestHelper.GetInt("pagesize", 0);
            int start = (pagenum + 1) * pagesize - pagesize;
            int end = (pagenum + 1) * pagesize + 1;

            string filter = XQGridHelper.GetFiltering();//获取过滤条件
            string sort = XQGridHelper.GetSort();//排序条件
            string sortorder = RequestHelper.GetString("sortorder");//排序字段
            if (sortorder == "")
                sortorder = " order by ID ";
            string xkdm = RequestHelper.GetString("xkdm");//学科代码
            string year = RequestHelper.GetString("year");//年度
            UserInfo ui = BLL.Utils.GetCurrentUser();
            BoolModel bm = new BoolModel();
            if (ui == null)
            {
                bm.success = false;
                bm.title = "用户登录过期";
                bm.description = "用户未登录，请重新登录再试";
                return "";
            }
            string strWhere = string.Empty;
            if (!string.IsNullOrEmpty(year) && year != "0" && year != "1")
                strWhere = "and YEAR(GR_XSJL.qsrq)=" + year;
            if (!string.IsNullOrEmpty(xkdm) && xkdm != "-1")
                strWhere += string.Format("and GR_XSJL.XKDM= '{0}'", xkdm);
            //查询数据sql
            string sql = string.Format(@"
                                        SELECT   dbo.GR_XSJL.ID,GR_XSJL.XKDM,dbo.GR_XSJL.UserID,dbo.GR_XSJL.dwmc, dbo.GR_XSJL.qsrq,dbo.GR_XSJL.jsrq, dbo.dm_xsjllx.xsjllx,dbo.GR_XSJL.CHECKED
                                        FROM dbo.GR_XSJL LEFT OUTER JOIN
	                                    dbo.dm_xsjllx  on dbo.GR_XSJL.jllxm=dbo.dm_xsjllx.xsjllxm
                                        WHERE GR_XSJL.UserID = '{0}' {1}
                                        ", ui.Identity, strWhere);
            //格式化分页
            string page_sql = @" with temp as 
                                    (
                                         {0}
                                    )
                                    select {1} from (
                                    select ROW_NUMBER() over(order by qsrq desc) as rownum,* from temp 
                                    where 1=1 {2} {3}
                                    ) b";
            //分页控制
            string page_condition = string.Format(" where rownum>{0} and rownum<{1}", start, end);
            //读取当前页记录
            string exec_sql = string.Format(page_sql, sql, "*", filter, sort) + page_condition;


            DataTable dt = DbHelper.ExecuteDataSet(exec_sql).Tables[0];
            exec_sql = string.Format(page_sql, sql, "count(*)", filter, sort);
            object obj = DbHelper.ExecuteScalar(exec_sql);
            string json = JsonConvert.SerializeObject(dt);

            json = "{\"TotalRows\":" + ConvertHelper.StrToInt(obj) + ",\"Rows\":" + json + "}";

            return json;

        }

        public ActionResult XSJL_Info()
        {
            return View();
        }
        /// <summary>
        /// 学术交流类型数据
        /// </summary>
        /// <returns></returns>
        public string GetXSJLLX()
        {
            RepositoryBase<DM_XSJLLX> jb_context = new RepositoryBase<DM_XSJLLX>();
            List<DM_XSJLLX> list = jb_context.GetAll().ToList();

            var lx = from m in list
                     select new
                     {
                         m.XSJLLXM,
                         m.XSJLLX
                     };
            string json = JsonConvert.SerializeObject(lx);
            return json;
        }

        public JsonResult XSJLSaveData()
        {
            BoolModel bm = new BoolModel();
            RepositoryBase<GR_XSJL> gr_xsjl_context = new RepositoryBase<GR_XSJL>();

            UserInfo ui = BLL.Utils.GetCurrentUser();
            if (ui == null)
            {
                bm.success = false;
                bm.title = "用户登录过期";
                bm.description = "用户未登录，请重新登录再试";
                return Json(bm);
            }
            try
            {
                int id = RequestHelper.GetInt("ID", -1);
                if (id > 0)//修改科研经费支出情况
                {
                    #region 修改科研经费支出情况
                    GR_XSJL sourceModel = gr_xsjl_context.GetById(id);
                    GR_XSJL desModel = gr_xsjl_context.GetNewModelById(id);
                    desModel.JLLXM = RequestHelper.GetString("JLLXM");
                    desModel.GBM = RequestHelper.GetString("GBM");
                    string date = RequestHelper.GetString("QSRQ");
                    if (!string.IsNullOrEmpty(date))
                        desModel.QSRQ = DateTime.Parse(date);
                    date = RequestHelper.GetString("JSRQ");
                    if (!string.IsNullOrEmpty(date))
                        desModel.JSRQ = DateTime.Parse(date);
                    desModel.DWMC = RequestHelper.GetString("DWMC");
                    desModel.XMMC = RequestHelper.GetString("XMMC");

                    if (desModel.JLLXM != sourceModel.JLLXM || desModel.GBM != sourceModel.GBM || desModel.QSRQ != sourceModel.QSRQ || desModel.JSRQ != sourceModel.JSRQ || desModel.DWMC != sourceModel.DWMC)
                    {
                        var list = gr_xsjl_context.GetAll().Where("UserID=@0 and JLLXM=@1 and GBM=@2 and QSRQ=@3 and JSRQ=@4 and DWMC=@5", new object[] { ui.Identity, desModel.JLLXM, desModel.GBM, desModel.QSRQ, desModel.JSRQ, desModel.DWMC });
                        if (list.Count() > 0)
                        {
                            bm.success = false;
                            bm.title = "该学术交流情况已经存在，不可重复！";
                            bm.description = "";
                            return Json(bm);
                        }
                    }
                    gr_xsjl_context.Update(sourceModel, desModel);
                    gr_xsjl_context.SaveChanges();
                    #endregion
                }
                else//新增
                {
                    #region 新增
                    GR_XSJL desModel = new GR_XSJL();
                    desModel.UserID = ui.Identity;
                    desModel.XKDM = RequestHelper.GetString("XKDM");
                    //desModel.YXSDM = ui.UserGroup;
                    RepositoryBase<XK2UserID> xk2userid_context = new RepositoryBase<XK2UserID>();
                    var xk2userid_list = xk2userid_context.GetAll().Where(x => x.USERID == ui.Identity && x.XKDM == desModel.XKDM);
                    if (xk2userid_list.Count() > 0)
                        desModel.YXSDM = xk2userid_list.First().YXSDM;
                    desModel.JLLXM = RequestHelper.GetString("JLLXM");
                    desModel.GBM = RequestHelper.GetString("GBM");
                    string date = RequestHelper.GetString("QSRQ");
                    if (!string.IsNullOrEmpty(date))
                        desModel.QSRQ = DateTime.Parse(date);
                    date = RequestHelper.GetString("JSRQ");
                    if (!string.IsNullOrEmpty(date))
                        desModel.JSRQ = DateTime.Parse(date);
                    desModel.DWMC = RequestHelper.GetString("DWMC");
                    desModel.XMMC = RequestHelper.GetString("XMMC");

                    var list = gr_xsjl_context.GetAll().Where("UserID=@0 and JLLXM=@1 and GBM=@2 and QSRQ=@3 and JSRQ=@4 and DWMC=@5", new object[] { ui.Identity, desModel.JLLXM, desModel.GBM, desModel.QSRQ, desModel.JSRQ, desModel.DWMC });
                    if (list.Count() > 0)
                    {
                        bm.success = false;
                        bm.title = "该学术交流情况已经存在，不可重复添加！";
                        bm.description = "";
                        return Json(bm);
                    }
                    gr_xsjl_context.Add(desModel);
                    gr_xsjl_context.SaveChanges();
                    #endregion
                }
            }
            catch (Exception ex)
            {
                bm.success = false;
                bm.title = ex.Message;
                bm.description = "";
                return Json(bm);
            }

            bm.success = true;
            bm.title = "成功";
            bm.description = "";

            return Json(bm);
        }

        public JsonResult XSJLSaveMainData()
        {
            BoolModel bm = new BoolModel();
            RepositoryBase<GR_XSJL> gr_xsjl_context = new RepositoryBase<GR_XSJL>();

            UserInfo ui = BLL.Utils.GetCurrentUser();
            if (ui == null)
            {
                bm.success = false;
                bm.title = "用户登录过期";
                bm.description = "用户未登录，请重新登录再试";
                return Json(bm);
            }
            try
            {
                int id = RequestHelper.GetInt("ID", -1);
                if (id > 0)//修改科研经费支出情况
                {
                    #region 修改科研经费支出情况
                    GR_XSJL sourceModel = gr_xsjl_context.GetById(id);
                    GR_XSJL desModel = gr_xsjl_context.GetNewModelById(id);
                    desModel.JLLXM = RequestHelper.GetString("JLLXM");
                    desModel.GBM = RequestHelper.GetString("GBM");
                    string date = RequestHelper.GetString("QSRQ");
                    if (!string.IsNullOrEmpty(date))
                        desModel.QSRQ = DateTime.Parse(date);
                    date = RequestHelper.GetString("JSRQ");
                    if (!string.IsNullOrEmpty(date))
                        desModel.JSRQ = DateTime.Parse(date);
                    desModel.DWMC = RequestHelper.GetString("DWMC");
                    desModel.XMMC = RequestHelper.GetString("XMMC");

                    if (desModel.JLLXM != sourceModel.JLLXM || desModel.GBM != sourceModel.GBM || desModel.QSRQ != sourceModel.QSRQ || desModel.JSRQ != sourceModel.JSRQ || desModel.DWMC != sourceModel.DWMC)
                    {
                        var list = gr_xsjl_context.GetAll().Where("UserID=@0 and JLLXM=@1 and GBM=@2 and QSRQ=@3 and JSRQ=@4 and DWMC=@5", new object[] { desModel.UserID, desModel.JLLXM, desModel.GBM, desModel.QSRQ, desModel.JSRQ, desModel.DWMC });
                        if (list.Count() > 0)
                        {
                            bm.success = false;
                            bm.title = "该学术交流情况已经存在，不可重复！";
                            bm.description = "";
                            return Json(bm);
                        }
                    }
                    gr_xsjl_context.Update(sourceModel, desModel);
                    gr_xsjl_context.SaveChanges();
                    #endregion
                }
                else//新增
                {
                    bm.success = false;
                    bm.title = "记录不存在！";
                    bm.description = "";
                    return Json(bm);
                }
            }
            catch (Exception ex)
            {
                bm.success = false;
                bm.title = ex.Message;
                bm.description = "";
                return Json(bm);
            }

            bm.success = true;
            bm.title = "成功";
            bm.description = "";

            return Json(bm);
        }

        /// <summary>
        /// 删除学术交流
        /// </summary>
        /// <returns></returns>
        public JsonResult XSJLDeleteData()
        {
            int id = RequestHelper.GetInt("id", -1);
            RepositoryBase<GR_XSJL> xsjl_context = new RepositoryBase<GR_XSJL>();
            GR_XSJL model = xsjl_context.GetById(id);
            BoolModel bm = new BoolModel();
            UserInfo ui = BLL.Utils.GetCurrentUser();
            if (ui == null)
            {
                bm.success = false;
                bm.title = "用户登录过期";
                bm.description = "用户未登录，请重新登录再试";
                return Json(bm);
            }
            try
            {
                if (model != null)
                {
                    xsjl_context.Remove(model);
                    xsjl_context.SaveChanges();

                    bm.success = true;
                    bm.title = "成功";
                    bm.description = "成功";
                }
                else
                {
                    bm.success = false;
                    bm.title = "记录不存在！";
                    bm.description = "";
                }
            }
            catch (Exception ex)
            {
                bm.success = false;
                bm.title = ex.Message.Replace("\n", "");
                bm.description = "";
            }
            return Json(bm);

        }

        #endregion

        #region 参加会议

        public ActionResult XSHY_List()
        {
            return View();
        }
        /// <summary>
        /// 获取参加会议列表
        /// </summary>
        /// <returns></returns>
        public string GetXSHY_List()
        {
            int pagenum = RequestHelper.GetInt("pagenum", 0);//获取当前页
            int pagesize = RequestHelper.GetInt("pagesize", 0);
            int start = (pagenum + 1) * pagesize - pagesize;
            int end = (pagenum + 1) * pagesize + 1;

            string filter = XQGridHelper.GetFiltering();//获取过滤条件
            string sort = XQGridHelper.GetSort();//排序条件
            string sortorder = RequestHelper.GetString("sortorder");//排序字段
            if (sortorder == "")
                sortorder = " order by ID ";
            string xkdm = RequestHelper.GetString("xkdm");//学科代码
            string year = RequestHelper.GetString("year");//年度
            UserInfo ui = BLL.Utils.GetCurrentUser();
            BoolModel bm = new BoolModel();
            if (ui == null)
            {
                bm.success = false;
                bm.title = "用户登录过期";
                bm.description = "用户未登录，请重新登录再试";
                return "";
            }
            string strWhere = string.Empty;
            if (!string.IsNullOrEmpty(year) && year != "0" && year != "1")
                strWhere = "and YEAR(main_XSHY.CBRQ)=" + year;
            if (!string.IsNullOrEmpty(xkdm) && xkdm != "-1")
                strWhere += string.Format("and dbo.GR_XSHY.XKDM= '{0}'", xkdm);
            //查询数据sql
            string sql = string.Format(@"
                                        SELECT dbo.GR_XSHY.ID,dbo.GR_XSHY.UserID, dbo.GR_XSHY.XKDM,dbo.GR_XSHY.LINKID, 
                                                dbo.main_XSHY.HYMC, dbo.main_XSHY.HYDD, dbo.main_XSHY.JBDWMC,
	                                        dbo.DM_HYDJ.HYDJ,dbo.main_XSHY.CHECKED	
                                        FROM dbo.GR_XSHY 
	                                        LEFT OUTER JOIN
                                              dbo.main_XSHY ON dbo.GR_XSHY.LinkID = dbo.main_XSHY.ID
	                                        LEFT OUTER JOIN dbo.DM_HYDJ
                                                        ON  dbo.main_XSHY.HYDJM=dbo.DM_HYDJ.HYDJM
                                        WHERE dbo.GR_XSHY.UserID = '{0}' {1}
                                        ", ui.Identity, strWhere);
            //格式化分页
            string page_sql = @" with temp as 
                                    (
                                         {0}
                                    )
                                    select {1} from (
                                    select ROW_NUMBER() over(order by ID asc) as rownum,* from temp 
                                    where 1=1 {2} {3}
                                    ) b";
            //分页控制
            string page_condition = string.Format(" where rownum>{0} and rownum<{1}", start, end);
            //读取当前页记录
            string exec_sql = string.Format(page_sql, sql, "*", filter, sort) + page_condition;


            DataTable dt = DbHelper.ExecuteDataSet(exec_sql).Tables[0];
            exec_sql = string.Format(page_sql, sql, "count(*)", filter, sort);
            object obj = DbHelper.ExecuteScalar(exec_sql);
            string json = JsonConvert.SerializeObject(dt);

            json = "{\"TotalRows\":" + ConvertHelper.StrToInt(obj) + ",\"Rows\":" + json + "}";

            return json;

        }
        /// <summary>
        /// 获取搜索参加会议数据
        /// </summary>
        /// <returns></returns>
        public string GetXSHYSearchData()
        {
            int pagenum = RequestHelper.GetInt("pagenum", 0);//获取当前页
            int pagesize = RequestHelper.GetInt("pagesize", 0);
            int start = (pagenum + 1) * pagesize - pagesize;
            int end = (pagenum + 1) * pagesize + 1;
            string xkdm = RequestHelper.GetString("xkdm");
            string year = RequestHelper.GetString("year");
            string searchName = RequestHelper.GetString("search_data");

            SqlParameter countPara = new SqlParameter("@docount", false);
            SqlParameter[] myparm = new SqlParameter[] { new SqlParameter("@startIndex", start) ,
            new SqlParameter("@endIndex", end),
            new SqlParameter("@XKDM", xkdm),
            new SqlParameter("@ND", year),
            countPara,
            new SqlParameter("@MC", "%" + searchName + "%")};

            DataSet ds = DbHelper.ExecuteDataSetForProc("Search_XSHY", myparm);

            string json = JsonConvert.SerializeObject(ds.Tables[0]);

            countPara.Value = true;
            DataTable count_dt = DbHelper.ExecuteDataSetForProc("Search_XSHY", myparm).Tables[0];

            object obj = count_dt.Rows.Count > 0 ? count_dt.Rows[0][0] : 0;

            json = "{\"TotalRows\":" + ConvertHelper.StrToInt(obj) + ",\"Rows\":" + json + "}";

            return json;

        }
        public ActionResult XSHY_Info()
        {
            return View();
        }
        /// <summary>
        /// 获取会议等级数据
        /// </summary>
        /// <returns></returns>
        public string GetHYDJ()
        {
            RepositoryBase<DM_HYDJ> jb_context = new RepositoryBase<DM_HYDJ>();
            List<DM_HYDJ> list = jb_context.GetAll().ToList();

            var lx = from m in list
                     select new
                     {
                         m.HYDJM,
                         m.HYDJ
                     };
            string json = JsonConvert.SerializeObject(lx);
            return json;
        }
        /// <summary>
        /// 获取会议举办形式数据
        /// </summary>
        /// <returns></returns>
        public string GetHYJBXSM()
        {
            RepositoryBase<DM_HYJBXS> jb_context = new RepositoryBase<DM_HYJBXS>();
            List<DM_HYJBXS> list = jb_context.GetAll().ToList();

            var lx = from m in list
                     select new
                     {
                         m.HYJBXSM,
                         m.HYJBXS
                     };
            string json = JsonConvert.SerializeObject(lx);
            return json;
        }
        /// <summary>
        /// 获取与所选择与他人合作的参加会议
        /// </summary>
        /// <returns></returns>
        public JsonResult GetSelectXSHYDataRecord()
        {
            string id = RequestHelper.GetString("id");
            string sql = string.Format("SELECT t.*, CONVERT(varchar(10) , KSRQ, 120 ) KSRQDATE,CONVERT(varchar(10) , JSRQ, 120 ) JSRQDATE  FROM [main_XSHY] t WHERE [ID]='{0}'", id);
            BoolModel bm = new BoolModel();
            try
            {
                DataTable dt = DbHelper.ExecuteDataSet(sql).Tables[0];
                if (dt.Rows.Count > 0)
                {
                    bm.success = true;
                    bm.title = "成功";
                    bm.description = JsonConvert.SerializeObject(dt.Rows[0]);
                }
                else
                {
                    bm.success = false;
                    bm.title = "没有数据";
                    bm.description = "";
                }
            }
            catch (Exception ex)
            {
                bm.success = false;
                bm.title = ex.Message.Replace("\n", "");
                bm.description = "";
            }
            return Json(bm);
        }

        /// <summary>
        /// 获取参加会议细表信息
        /// </summary>
        /// <returns></returns>
        public JsonResult GetGR_XSHY_DataRecord()
        {
            string id = RequestHelper.GetString("id");
            string sql = string.Format("SELECT * FROM [GR_XSHY] WHERE  [ID]='{0}'", id);
            BoolModel bm = new BoolModel();
            try
            {
                DataTable dt = DbHelper.ExecuteDataSet(sql).Tables[0];
                if (dt.Rows.Count > 0)
                {
                    bm.success = true;
                    bm.title = "成功";
                    bm.description = JsonConvert.SerializeObject(dt.Rows[0]);
                }
                else
                {
                    bm.success = false;
                    bm.title = "没有数据";
                    bm.description = "";
                }
            }
            catch (Exception ex)
            {
                bm.success = false;
                bm.title = ex.Message.Replace("\n", "");
                bm.description = "";
            }
            return Json(bm);
        }
        public JsonResult XSHYSaveData()
        {
            BoolModel bm = new BoolModel();
            RepositoryBase<main_XSHY> main_kjzz_context = new RepositoryBase<main_XSHY>();
            RepositoryBase<GR_XSHY> gr_kjzz_context = new RepositoryBase<GR_XSHY>();

            UserInfo ui = BLL.Utils.GetCurrentUser();
            if (ui == null)
            {
                bm.success = false;
                bm.title = "用户登录过期";
                bm.description = "用户未登录，请重新登录再试";
                return Json(bm);
            }
            try
            {
                int gr_kjzz_id = RequestHelper.GetInt("GRID", -1);
                int main_kjzz_id = RequestHelper.GetInt("MainID", -1);
                if (gr_kjzz_id > 0)//修改论文
                {
                    #region 修改论文信息
                    main_XSHY sourceMainModel = main_kjzz_context.GetById(main_kjzz_id);
                    main_XSHY main = main_kjzz_context.GetNewModelById(main_kjzz_id);
                    if (main == null)
                        throw new Exception("未找到参加会议记录信息！");
                    main.XKDM = RequestHelper.GetString("XKDM");
                    main.HYMC = RequestHelper.GetString("HYMC");
                    main.HYDD = RequestHelper.GetString("HYDD");
                    main.CJRS = RequestHelper.GetInt("CJRS", 0);
                    string date = RequestHelper.GetString("KSRQ");
                    if (!string.IsNullOrEmpty(date))
                        main.KSRQ = DateTime.Parse(date);
                    date = RequestHelper.GetString("JSRQ");
                    if (!string.IsNullOrEmpty(date))
                        main.JSRQ = DateTime.Parse(date);
                    main.HYDJM = RequestHelper.GetString("HYDJM");
                    main.HYJBXSM = RequestHelper.GetString("HYJBXSM");
                    main.JBDWMC = RequestHelper.GetString("JBDWMC");
                    main.CBDWMC = RequestHelper.GetString("CBDWMC");
                    main.GBM = RequestHelper.GetString("GBM");
                    main.FBLWMC = RequestHelper.GetString("FBLWMC");
                    main_kjzz_context.Update(sourceMainModel, main);
                    //更新细表
                    GR_XSHY source_gr = gr_kjzz_context.GetById(gr_kjzz_id);
                    GR_XSHY gr = gr_kjzz_context.GetNewModelById(gr_kjzz_id);
                    gr.FBLWMC = RequestHelper.GetString("FBLWMC");
                    gr.BGMC = RequestHelper.GetString("BGMC");
                    gr_kjzz_context.Update(source_gr, gr);
                    main_kjzz_context.SaveChanges();
                    gr_kjzz_context.SaveChanges();
                    #endregion
                }
                else if (main_kjzz_id > 0)//修改被收录论文名称
                {
                    #region 修改被收录论文名称
                    main_XSHY sourceMainModel = main_kjzz_context.GetById(main_kjzz_id);
                    main_XSHY main = main_kjzz_context.GetNewModelById(main_kjzz_id);
                    main.FBLWMC = RequestHelper.GetString("FBLWMC");
                    main_kjzz_context.Update(sourceMainModel, main);
                    var list = gr_kjzz_context.GetAll().Where("UserID=@0 and XKDM=@1 and linkID=@2", new object[] { ui.Identity, main.XKDM, main.ID });
                    if (list.Count() > 0)//修改
                    {
                        GR_XSHY gr = list.First<GR_XSHY>();
                        GR_XSHY model = gr_kjzz_context.GetNewModelById(gr.ID);
                        model.FBLWMC = RequestHelper.GetString("FBLWMC");
                        model.BGMC = RequestHelper.GetString("BGMC");
                        gr_kjzz_context.Update(gr, model);
                    }
                    else//新增
                    {
                        GR_XSHY gr = new GR_XSHY();
                        gr.UserID = ui.Identity;
                        RepositoryBase<XK2UserID> xk2userid_context = new RepositoryBase<XK2UserID>();
                        var xk2userid_list = xk2userid_context.GetAll().Where(x => x.USERID == ui.Identity && x.XKDM == main.XKDM);
                        if (xk2userid_list.Count() > 0)
                            gr.YXSDM = xk2userid_list.First().YXSDM;
                        //gr.YXSDM = ui.UserGroup;
                        gr.XKDM = main.XKDM;
                        gr.LinkID = main.ID;
                        gr.FBLWMC = RequestHelper.GetString("FBLWMC");
                        gr.BGMC = RequestHelper.GetString("BGMC");
                        gr_kjzz_context.Add(gr);
                    }
                    main_kjzz_context.SaveChanges();
                    gr_kjzz_context.SaveChanges();
                    #endregion
                }
                else//新增
                {
                    #region 新增
                    main_XSHY main = new main_XSHY();
                    main.XKDM = RequestHelper.GetString("XKDM");
                    main.HYMC = RequestHelper.GetString("HYMC");
                    main.HYDD = RequestHelper.GetString("HYDD");
                    main.CJRS = RequestHelper.GetInt("CJRS", 0);
                    string date = RequestHelper.GetString("KSRQ");
                    if (!string.IsNullOrEmpty(date))
                        main.KSRQ = DateTime.Parse(date);
                    date = RequestHelper.GetString("JSRQ");
                    if (!string.IsNullOrEmpty(date))
                        main.JSRQ = DateTime.Parse(date);
                    main.HYDJM = RequestHelper.GetString("HYDJM");
                    main.HYJBXSM = RequestHelper.GetString("HYJBXSM");
                    main.JBDWMC = RequestHelper.GetString("JBDWMC");
                    main.CBDWMC = RequestHelper.GetString("CBDWMC");
                    main.GBM = RequestHelper.GetString("GBM");
                    main.FBLWMC = RequestHelper.GetString("FBLWMC");
                    var list = main_kjzz_context.GetAll().Where("HYMC=@0 and HYDD=@1 and KSRQ=@2 and JSRQ=@3", new object[] { RequestHelper.GetString("HYMC"), RequestHelper.GetString("HYDD"), DateTime.Parse(RequestHelper.GetString("KSRQ")), DateTime.Parse(RequestHelper.GetString("JSRQ")) });
                    if (list.Count() > 0)
                    {
                        bm.success = false;
                        bm.title = "";
                        bm.description = string.Format("会议名称：{0}，会议地点：{1}，开始日期：{2}，结束日期：{3} 已存在，请使用与他人合作方式进行添加！", RequestHelper.GetString("HYMC"), RequestHelper.GetString("HYDD"), DateTime.Parse(RequestHelper.GetString("KSRQ")), DateTime.Parse(RequestHelper.GetString("JSRQ")));
                        return Json(bm);
                    }
                    main_kjzz_context.Add(main);
                    main_kjzz_context.SaveChanges();
                    //此处重新处理 main_XSHY 表记录，原有记录不合理
                    if (list.Count() > 0)
                    {
                        int main_id = list.First<main_XSHY>().ID;
                        GR_XSHY gr = new GR_XSHY();
                        gr.UserID = ui.Identity;
                        RepositoryBase<XK2UserID> xk2userid_context = new RepositoryBase<XK2UserID>();
                        var xk2userid_list = xk2userid_context.GetAll().Where(x => x.USERID == ui.Identity && x.XKDM == main.XKDM);
                        if (xk2userid_list.Count() > 0)
                            gr.YXSDM = xk2userid_list.First().YXSDM;
                        //gr.YXSDM = ui.UserGroup;
                        gr.XKDM = main.XKDM;
                        gr.LinkID = main.ID;
                        gr.FBLWMC = RequestHelper.GetString("FBLWMC");
                        gr.BGMC = RequestHelper.GetString("BGMC");
                        gr_kjzz_context.Add(gr);
                        gr_kjzz_context.SaveChanges();
                    }
                    #endregion
                }
            }
            catch (Exception ex)
            {
                bm.success = false;
                bm.title = ex.Message;
                bm.description = "";
                return Json(bm);
            }

            bm.success = true;
            bm.title = "成功";
            bm.description = "";

            return Json(bm);
        }

        public JsonResult XSHYSaveMainData()
        {
            BoolModel bm = new BoolModel();
            RepositoryBase<main_XSHY> main_kjzz_context = new RepositoryBase<main_XSHY>(); 

            UserInfo ui = BLL.Utils.GetCurrentUser();
            if (ui == null)
            {
                bm.success = false;
                bm.title = "用户登录过期";
                bm.description = "用户未登录，请重新登录再试";
                return Json(bm);
            }
            try
            { 
                int main_kjzz_id = RequestHelper.GetInt("MainID", -1);
                if (main_kjzz_id > 0)//修改论文
                {
                    #region 修改论文信息
                    main_XSHY sourceMainModel = main_kjzz_context.GetById(main_kjzz_id);
                    main_XSHY main = main_kjzz_context.GetNewModelById(main_kjzz_id);
                    if (main == null)
                        throw new Exception("未找到参加会议记录信息！");
                    main.XKDM = RequestHelper.GetString("XKDM");
                    main.HYMC = RequestHelper.GetString("HYMC");
                    main.HYDD = RequestHelper.GetString("HYDD");
                    main.CJRS = RequestHelper.GetInt("CJRS", 0);
                    string date = RequestHelper.GetString("KSRQ");
                    if (!string.IsNullOrEmpty(date))
                        main.KSRQ = DateTime.Parse(date);
                    date = RequestHelper.GetString("JSRQ");
                    if (!string.IsNullOrEmpty(date))
                        main.JSRQ = DateTime.Parse(date);
                    main.HYDJM = RequestHelper.GetString("HYDJM");
                    main.HYJBXSM = RequestHelper.GetString("HYJBXSM");
                    main.JBDWMC = RequestHelper.GetString("JBDWMC");
                    main.CBDWMC = RequestHelper.GetString("CBDWMC");
                    main.GBM = RequestHelper.GetString("GBM");
                    main.FBLWMC = RequestHelper.GetString("FBLWMC");
                    main_kjzz_context.Update(sourceMainModel, main); 
                    main_kjzz_context.SaveChanges(); 
                    #endregion
                } 
                else//新增
                {
                    bm.success = false;
                    bm.title = "记录不存在！";
                    bm.description = "";
                    return Json(bm);
                }
            }
            catch (Exception ex)
            {
                bm.success = false;
                bm.title = ex.Message;
                bm.description = "";
                return Json(bm);
            }

            bm.success = true;
            bm.title = "成功";
            bm.description = "";

            return Json(bm);
        }
        /// <summary>
        /// 删除参加学术会议情况记录
        /// </summary>
        /// <returns></returns>
        public JsonResult XSHYDeleteData()
        {
            int gr_id = RequestHelper.GetInt("gr_id", -1);
            int main_id = RequestHelper.GetInt("main_id", -1);
            RepositoryBase<main_XSHY> main_context = new RepositoryBase<main_XSHY>();
            RepositoryBase<GR_XSHY> gr_context = new RepositoryBase<GR_XSHY>();
            BoolModel bm = new BoolModel();
            UserInfo ui = BLL.Utils.GetCurrentUser();
            if (ui == null)
            {
                bm.success = false;
                bm.title = "用户登录过期";
                bm.description = "用户未登录，请重新登录再试";
                return Json(bm);
            }
            try
            {
                main_XSHY sourceMain = main_context.GetById(main_id);
                GR_XSHY gr = gr_context.GetById(gr_id);
                if (sourceMain != null && gr != null)
                {
                    var gr_list = from m in gr_context.GetAll().ToList()
                                  where m.LinkID == main_id
                                  select m;
                    if (gr_list.Count() == 1)//说明只有当前操作员拥有此论文信息，可以删除论文信息
                    {
                        main_context.Remove(sourceMain);
                    }
                    else
                    {
                        gr_context.Remove(gr);//当主记录不删除时需要单独删除细表对象，若主记录删除，则会自动把细表删除，因为主表和细表有外键关联
                    }

                    main_context.SaveChanges();
                    gr_context.SaveChanges();

                    bm.success = true;
                    bm.title = "成功";
                    bm.description = "成功";
                }
                else
                {
                    bm.success = false;
                    bm.title = "记录不存在！";
                    bm.description = "";
                }
            }
            catch (Exception ex)
            {
                bm.success = false;
                bm.title = ex.Message.Replace("\n", "");
                bm.description = "";
            }
            return Json(bm);

        }
        #endregion

        #region JBXSHY 学院举办学术会议

        [HttpPost]
        public string GetHYDJList()
        {
            string sql = @" Select * from [DM_HYDJ]";
            DataTable dt = DbHelper.ExecuteDataSet(sql).Tables[0];
            object obj = DbHelper.ExecuteScalar(sql);
            string json = JsonConvert.SerializeObject(dt);
            return json;
        }
        public string GetXY_JBXSHY_List()
        {

            int pagenum = RequestHelper.GetInt("pagenum", 0);//获取当前页
            int pagesize = RequestHelper.GetInt("pagesize", 0);
            int start = (pagenum + 1) * pagesize - pagesize;
            int end = (pagenum + 1) * pagesize + 1;

            string filter = XQGridHelper.GetFiltering();//获取过滤条件
            string sort = XQGridHelper.GetSort();//排序条件 
            string yxs = RequestHelper.GetString("YXS");
            UserInfo ui = BLL.Utils.GetCurrentUser();
            string condition = "";
            if (!string.IsNullOrEmpty(yxs))
                condition = string.Format(" and h.YXSDM = '{0}'", yxs);
            if (!ui.SuperAdmin)
                condition += string.Format("and h.YXSDM in (select YXSDM from YXS_LINK where UserID='{0}') ",ui.Identity);

            string sql = @" with temp as 
                            (
	                            SELECT h.ID, h.YXSDM AS 院系所代码, y.YXSMC AS 院系所名称, h.HYMC AS 会议名称, 
	                            h.HYDD AS 会议地点, CONVERT(varchar(100),h.KSRQ,23) as KSRQ, CONVERT(varchar(100),h.JSRQ,23) as JSRQ, h.HYDJ AS 会议等级, 
	                            h.CJRS AS 参加人数
	                            FROM XY_JBXSHY h LEFT OUTER JOIN
	                            YXS_JBXX y ON h.YXSDM = y.YXSH   
	                            where 1=1 {0}
                            )
                            select {1} from (
                            select ROW_NUMBER() over(order by KSRQ desc) as rownum,* from temp 
                            where 1=1 {2} {3}
                            ) b  ";

            string page_condition = string.Format(" where rownum>{0} and rownum<{1}", start, end);


            string exec_sql = string.Format(sql, condition, "*", filter, "") + page_condition;

            DataTable dt = DbHelper.ExecuteDataSet(exec_sql).Tables[0];
            exec_sql = string.Format(sql, condition, "count(*)", filter, "");
            object obj = DbHelper.ExecuteScalar(exec_sql);
            string json = JsonConvert.SerializeObject(dt);

            json = "{\"TotalRows\":" + ConvertHelper.StrToInt(obj) + ",\"Rows\":" + json + "}";

            return json;

        }
        public JsonResult SaveXY_JBXSHY()
        {

            BoolModel bm = new BoolModel();
            RepositoryBase<XY_JBXSHY> jb_context = new RepositoryBase<XY_JBXSHY>();
            UserInfo ui = BLL.Utils.GetCurrentUser();

            if (ui == null)
            {

                bm.success = false;
                bm.title = "用户登录过期";
                bm.description = "用户未登录，请重新登录再试";
                return Json(bm);
            }

            try
            {

                XY_JBXSHY edit = new Models.XY_JBXSHY();
                edit.HYMC = RequestHelper.GetString("会议名称");
                edit.HYDD = RequestHelper.GetString("会议地点");
                edit.KSRQ = Convert.ToDateTime(RequestHelper.GetString("KSRQ"));
                edit.JSRQ = Convert.ToDateTime(RequestHelper.GetString("JSRQ"));
                edit.HYDJ = RequestHelper.GetString("会议等级");
                edit.CJRS = Convert.ToInt32(RequestHelper.GetString("参加人数"));
                edit.ZBDW = RequestHelper.GetString("主办单位");
                edit.LWSL = Convert.ToInt16(RequestHelper.GetString("论文数量"));
                //edit.YXSDM = ui.UserGroup; //存储过程CheckUser 取得UserGroup信息
                edit.YXSDM = RequestHelper.GetString("YXS");


                if (String.IsNullOrEmpty(RequestHelper.GetString("ID")))
                {
                    jb_context.Add(edit);
                }
                else
                {
                    edit.ID = Convert.ToInt32(RequestHelper.GetString("ID"));

                    XY_JBXSHY jb = jb_context.GetById(edit.ID);
                    if (jb != null)
                    {
                        jb_context.Update(jb, edit);
                    }
                }

                jb_context.SaveChanges();
            }

            catch (DbEntityValidationException ex)
            {

                bm.success = false;
                bm.title = ex.Message;
                bm.description = "";
                return Json(bm);

            }

            catch (Exception ex)
            {

                bm.success = false;
                bm.title = ex.Message;
                bm.description = "";
                return Json(bm);

            }


            bm.success = true;
            bm.title = "成功";
            bm.description = "";

            return Json(bm);

        }

        public JsonResult DeleteXY_JBXSHY()
        {

            BoolModel bm = new BoolModel();
            RepositoryBase<XY_JBXSHY> jb_context = new RepositoryBase<XY_JBXSHY>();
            try
            {

                string ids = RequestHelper.GetString("ids");
                List<dynamic> list = JsonConvert.DeserializeObject<List<dynamic>>(ids);
                for (int i = 0; i < list.Count; i++)
                {

                    XY_JBXSHY mi = jb_context.GetById(Convert.ToInt32(list[i].ToString()));
                    if (mi == null)
                    {
                        continue;
                    }

                    jb_context.Remove(mi);
                }
                jb_context.SaveChanges();
            }
            catch (Exception ex)
            {
                bm.success = false;
                bm.title = ex.Message;
                bm.description = "";
                return Json(bm);

            }

            bm.success = true;
            bm.title = "成功";
            bm.description = "";
            return Json(bm);

        }

        #endregion

        #region JBXSHY 学科举办学术会议情况操作

        [HttpPost]
        public string GetXK_JBXSHY_List()
        {

            int pagenum = RequestHelper.GetInt("pagenum", 0);//获取当前页
            int pagesize = RequestHelper.GetInt("pagesize", 0);
            int start = (pagenum + 1) * pagesize - pagesize;
            int end = (pagenum + 1) * pagesize + 1;

            string filter = XQGridHelper.GetFiltering();//获取过滤条件
            string sort = XQGridHelper.GetSort();//排序条件
            string sortorder = RequestHelper.GetString("sortorder");//排序字段
            string xkdm = RequestHelper.GetString("XKDM");
            string yxs = RequestHelper.GetString("YXS");
            string condition = "";
            if (!string.IsNullOrEmpty(yxs))
                condition = string.Format(" and xk.YXSDM = '{0}'", yxs); 
            if (!string.IsNullOrEmpty(xkdm))
                condition += string.Format(" and xk.XKDM = '{0}'", xkdm);


            string sql = @" with temp as 
                            (
	                            SELECT xk.ID, xy.YXSDM AS 院系所代码,(select top 1 XKMC  from XY_EJXK where XKDM=xk.XKDM) 学科名称, y.YXSMC AS 院系所名称, xy.HYMC AS 会议名称, 
	                            xy.HYDD AS 会议地点, CONVERT(varchar(100),xy.KSRQ,23) as KSRQ, CONVERT(varchar(100),xy.JSRQ,23) AS 结束日期, xy.HYDJ AS 会议等级, 
	                            xy.CJRS AS 参加人数
	                            FROM XK_JBXSHY xk 
	                            LEFT JOIN
	                            XY_JBXSHY xy 
	                            on xk.LinkID=xy.ID
	                            left outer join
	                            YXS_JBXX y ON xy.YXSDM = y.YXSH
	                            where 1=1 {0}
                            )
                            select {1} from (
                            select ROW_NUMBER() over(order by 院系所代码,学科名称,KSRQ desc) as rownum,* from temp 
                            where 1=1 {2} {3}
                            ) b  ";

            string page_condition = string.Format(" where rownum>{0} and rownum<{1}", start, end);


            string exec_sql = string.Format(sql, condition, "*", filter, "") + page_condition;

            DataTable dt = DbHelper.ExecuteDataSet(exec_sql).Tables[0];
            exec_sql = string.Format(sql, condition, "count(*)", filter, "");
            object obj = DbHelper.ExecuteScalar(exec_sql);
            string json = JsonConvert.SerializeObject(dt);

            json = "{\"TotalRows\":" + ConvertHelper.StrToInt(obj) + ",\"Rows\":" + json + "}";

            return json;

        }
        public string GetXY_NotSet_JBXSHY_List()
        {
            int pagenum = RequestHelper.GetInt("pagenum", 0);//获取当前页
            int pagesize = RequestHelper.GetInt("pagesize", 0);
            int start = (pagenum + 1) * pagesize - pagesize;
            int end = (pagenum + 1) * pagesize + 1;

            string filter = XQGridHelper.GetFiltering();//获取过滤条件
            string sort = XQGridHelper.GetSort();//排序条件
            string sortorder = RequestHelper.GetString("sortorder");//排序字段
            string yxs = RequestHelper.GetString("YXS");
            string xkdm = RequestHelper.GetString("XKDM");
            string condition_yxs = "";
            string condition_xkdm = "";
            if (!string.IsNullOrEmpty(yxs))
                condition_yxs = string.Format(" and xy.YXSDM = '{0}'", yxs);
            if (!string.IsNullOrEmpty(xkdm))
                condition_xkdm = string.Format(" and xk.XKDM = '{0}'", xkdm);

            string sql = @"with temp as 
                            (
	                            SELECT xy.ID, xy.YXSDM AS 院系所代码, YXS_JBXX.YXSMC AS 院系所名称, xy.HYMC AS 会议名称, 
	                            xy.HYDD AS 会议地点, CONVERT(varchar(100),xy.KSRQ,23) as KSRQ, CONVERT(varchar(100),xy.JSRQ,23) as JSRQ, xy.HYDJ AS 会议等级, 
	                            xy.CJRS AS 参加人数
	                            FROM XY_JBXSHY xy LEFT OUTER JOIN
	                            YXS_JBXX ON xy.YXSDM = YXS_JBXX.YXSH
	                            where not exists (select 1 from XK_JBXSHY xk where xk.LinkID=xy.ID {0}	) {1}	                             
                            )
                            select {2} from (
                            select ROW_NUMBER() over(order by 院系所代码,ID asc) as rownum,* from temp 
                            where 1=1 {3} {4}
                            ) b ";


            UserInfo ui = BLL.Utils.GetCurrentUser();
            BoolModel bm = new BoolModel();
            if (ui == null)
            {
                bm.success = false;
                bm.title = "用户登录过期";
                bm.description = "用户未登录，请重新登录再试";
                return "";
            }
            string page_condition = string.Format(" where rownum>{0} and rownum<{1}", start, end);
            string exec_sql = string.Format(sql, condition_xkdm, condition_yxs, "*", filter, "") + page_condition;

            DataTable dt = DbHelper.ExecuteDataSet(exec_sql).Tables[0];
            exec_sql = string.Format(sql, condition_xkdm, condition_yxs, "count(*)", filter, "");
            object obj = DbHelper.ExecuteScalar(exec_sql);
            string json = JsonConvert.SerializeObject(dt);

            json = "{\"TotalRows\":" + ConvertHelper.StrToInt(obj) + ",\"Rows\":" + json + "}";

            return json;

        }
        public JsonResult SaveXKJBXSHY()
        {
            BoolModel bm = new BoolModel();
            RepositoryBase<XK_JBXSHY> jb_context = new RepositoryBase<XK_JBXSHY>();
            UserInfo ui = BLL.Utils.GetCurrentUser();

            if (ui == null)
            {
                bm.success = false;
                bm.title = "用户登录过期";
                bm.description = "用户未登录，请重新登录再试";
                return Json(bm);
            }

            try
            {
                string xkdm=RequestHelper.GetString("XKDM");
                string yxs= RequestHelper.GetString("YXS");
                 

                string ids = RequestHelper.GetString("ids");
                List<dynamic> list = JsonConvert.DeserializeObject<List<dynamic>>(ids);
                for (int i = 0; i < list.Count; i++)
                {
                    int new_id = int.Parse(list[i].ToString());
                    var l = jb_context.GetAll().Where(a => a.LinkID == new_id && a.YXSDM == yxs && a.XKDM == xkdm);
                    if (l.Count() == 0)
                    {
                        XK_JBXSHY edit = new Models.XK_JBXSHY();
                        edit.XKDM = xkdm;
                        edit.YXSDM = yxs;
                        edit.LinkID = Convert.ToInt32(list[i].ToString());
                        edit.ZDXK = 0;
                        edit.YJXK = 0;
                        edit.XKSel = 1;
                        edit.XXSel = 0;
                        edit.YJXKSel = 0;
                        edit.YJZDSel = 0;
                        jb_context.Add(edit);
                    }
                    jb_context.SaveChanges();
                }
            }

            catch (DbEntityValidationException ex)
            {

                bm.success = false;
                bm.title = ex.Message;
                bm.description = "";
                return Json(bm);

            }

            catch (Exception ex)
            {

                bm.success = false;
                bm.title = ex.Message;
                bm.description = "";
                return Json(bm);

            }


            bm.success = true;
            bm.title = "成功";
            bm.description = "";

            return Json(bm);
        }

        public JsonResult DeleteXK_JBXSHY()
        {

            BoolModel bm = new BoolModel();
            RepositoryBase<XK_JBXSHY> jb_context = new RepositoryBase<XK_JBXSHY>();
            try
            {
                string ids = RequestHelper.GetString("ids");
                List<dynamic> list = JsonConvert.DeserializeObject<List<dynamic>>(ids);
                for (int i = 0; i < list.Count; i++)
                {
                    XK_JBXSHY mi = jb_context.GetById(Convert.ToInt32(list[i].ToString()));
                    if (mi == null)
                    {
                        continue;
                    }
                    jb_context.Remove(mi);
                }
                jb_context.SaveChanges();
            }
            catch (Exception ex)
            {
                bm.success = false;
                bm.title = ex.Message;
                bm.description = "";
                return Json(bm);
            }

            bm.success = true;
            bm.title = "成功";
            bm.description = "";
            return Json(bm);

        }

        #endregion

        #region JBXSHY 个人填写举办会议情况
        [HttpPost]
        public string GetJBXSHY_XK()
        {
            UserInfo ui = BLL.Utils.GetCurrentUser();
            BoolModel bm = new BoolModel();

            if (ui == null)
            {
                bm.success = false;
                bm.title = "用户登录过期";
                bm.description = "用户未登录，请重新登录再试";
                return "";
            }
            string linkid = RequestHelper.GetString("LINKID");
            if (string.IsNullOrEmpty(linkid))
                linkid = "";
            else
                linkid = string.Format(" and xk.linkid = '{0}'", linkid);

            string sql = string.Empty;
            sql = string.Format(@"
                                    select distinct xk.XKDM,XY_EJXK.XKMC,xk.YXSDM,YXS_JBXX.YXSMC,case when xy_xk.xkdm is null then '1' else '0' end as CHECKED
                                    from XK_JBXSHY xk
                                    left join XY_EJXK on XY_EJXK.XKDM=xk.XKDM
                                    LEFT JOIN YXS_JBXX ON xk.YXSDM=YXS_JBXX.YXSH
									left join (select distinct xkdm,yxsdm from xk2userid where userid='{0}') as xy_xk on xy_xk.xkdm=xk.xkdm and xy_xk.yxsdm=xk.yxsdm
                                    where ISNULL(YXS_JBXX.YXSMC,'')<>'' and isnull(xy_ejxk.xkmc,'')<>'' {1}
                                    order by xk.XKDM
                                    ", ui.Identity, linkid);
            DataTable dt = DbHelper.ExecuteDataSet(sql).Tables[0];

            string json = JsonConvert.SerializeObject(dt);

            return json;

        }
        public string GetGR_JBXSHY_List()
        {

            int pagenum = RequestHelper.GetInt("pagenum", 0);//获取当前页
            int pagesize = RequestHelper.GetInt("pagesize", 0);
            int start = (pagenum + 1) * pagesize - pagesize;
            int end = (pagenum + 1) * pagesize + 1;

            string filter = XQGridHelper.GetFiltering();//获取过滤条件
            string sort = XQGridHelper.GetSort();//排序条件 
            UserInfo ui = BLL.Utils.GetCurrentUser();

            string sql = @" with temp as 
                            (
	                            SELECT h.ID, h.YXSDM AS 院系所代码, h.HYMC AS 会议名称, 
	                            h.HYDD AS 会议地点, CONVERT(varchar(100),h.KSRQ,23) as KSRQ, CONVERT(varchar(100),h.JSRQ,23) as JSRQ, h.HYDJ AS 会议等级, 
	                            h.CJRS AS 参加人数,h.ZBDW AS 主办单位,h.LWSL AS 论文数量
	                            FROM XY_JBXSHY h 
	                            where h.USERID='{0}'
                            )
                            select {1} from (
                            select ROW_NUMBER() over(order by KSRQ desc) as rownum,* from temp 
                            where 1=1 {2} {3}
                            ) b  ";

            string page_condition = string.Format(" where rownum>{0} and rownum<{1}", start, end);


            string exec_sql = string.Format(sql, ui.Identity, "*", filter, "") + page_condition;

            DataTable dt = DbHelper.ExecuteDataSet(exec_sql).Tables[0];
            exec_sql = string.Format(sql, ui.Identity, "count(*)", filter, "");
            object obj = DbHelper.ExecuteScalar(exec_sql);
            string json = JsonConvert.SerializeObject(dt);

            json = "{\"TotalRows\":" + ConvertHelper.StrToInt(obj) + ",\"Rows\":" + json + "}";

            return json;

        }
        public JsonResult SaveGR_JBXSHY()
        {
            BoolModel bm = new BoolModel();
            RepositoryBase<XY_JBXSHY> xy_context = new RepositoryBase<XY_JBXSHY>();
            RepositoryBase<XK_JBXSHY> xk_context = new RepositoryBase<XK_JBXSHY>();
            UserInfo ui = BLL.Utils.GetCurrentUser();
            if (ui == null)
            {
                bm.success = false;
                bm.title = "用户登录过期";
                bm.description = "用户未登录，请重新登录再试";
                return Json(bm);
            }

            try
            {
                XY_JBXSHY edit = new Models.XY_JBXSHY();
                edit.HYMC = RequestHelper.GetString("会议名称");
                edit.HYDD = RequestHelper.GetString("会议地点");
                edit.KSRQ = Convert.ToDateTime(RequestHelper.GetString("KSRQ"));
                edit.JSRQ = Convert.ToDateTime(RequestHelper.GetString("JSRQ"));
                edit.HYDJ = RequestHelper.GetString("会议等级");
                if (!String.IsNullOrEmpty(RequestHelper.GetString("参加人数")))
                {
                    edit.CJRS = Convert.ToInt32(RequestHelper.GetString("参加人数"));
                }
                edit.ZBDW = RequestHelper.GetString("主办单位");
                if (!String.IsNullOrEmpty(RequestHelper.GetString("论文数量")))
                {
                    edit.LWSL = Convert.ToInt16(RequestHelper.GetString("论文数量"));
                }
                //edit.YXSDM = ui.UserGroup; //存储过程CheckUser 取得UserGroup信息
                string yxsdm = RequestHelper.GetString("YXS");
                if (string.IsNullOrEmpty(yxsdm))
                {
                    yxsdm = ui.UserGroup;
                }
                edit.YXSDM = yxsdm;
                edit.USERID = ui.Identity;

                if (String.IsNullOrEmpty(RequestHelper.GetString("ID")))
                {
                    xy_context.Add(edit);
                }
                else
                {
                    edit.ID = Convert.ToInt32(RequestHelper.GetString("ID"));

                    XY_JBXSHY jb = xy_context.GetById(edit.ID);
                    if (jb != null)
                    {
                        xy_context.Update(jb, edit);
                    }
                }
                xy_context.SaveChanges();
                //共享学科(操作方法,首先按linkid先删除，然后按新的学科专业添加)
                if (xk_context.GetMany(a => a.LinkID == edit.ID).Count() > 0)
                    xk_context.Remove(a => a.LinkID == edit.ID);
                string record = RequestHelper.GetString("XKDMS");
                List<dynamic> list = JsonConvert.DeserializeObject<List<dynamic>>(record);
                for (int i = 0; i < list.Count; i++)
                {//[XKDM],[YXSDM],[LinkID]
                    XK_JBXSHY ssxk = new XK_JBXSHY();
                    ssxk.XKDM = list[i].XKDM;
                    ssxk.YXSDM = list[i].YXSDM;
                    ssxk.LinkID = edit.ID;
                    xk_context.Add(ssxk);
                }
                xk_context.SaveChanges();
            
            }
            catch (DbEntityValidationException ex)
            {
                bm.success = false;
                bm.title = ex.Message;
                bm.description = "";
                return Json(bm);

            }
            catch (Exception ex)
            {
                bm.success = false;
                bm.title = ex.Message;
                bm.description = "";
                return Json(bm);

            }
            bm.success = true;
            bm.title = "成功";
            bm.description = "";

            return Json(bm);

        }


        #endregion
    }
}
