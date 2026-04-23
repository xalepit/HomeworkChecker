/* 2452769 计科 幸可函 */
#pragma once
#include <ctime>
#include <iostream>
#include <iomanip>
#include <fstream>
#include <sstream>
#include <string>
#include <vector>
#include "../include/class_cft.h"
#include "../include/common_string_utils.h"
#include "../include/common_exe_runner.h"
#include "../include/class_txt_compare.h"
/******************************************
	全局常量定义
******************************************/
static const int HW_ITEMS_DEFAULT = 0;			     //指定测试项的总数默认值
static const int HW_ITEMS_MIN = 1;				     //指定测试项的总数最小值
static const int HW_ITEMS_MAX = 1024;			     //指定测试项的总数最大值
static const int HW_TIMEOUT_DEFAULT = 1;	 		 //超时时间默认值，单位为秒
static const int HW_TIMEOUT_MIN = 1;				 //超时时间最小值，单位为秒
static const int HW_TIMEOUT_MAX = 3600;				 //超时时间最大值，单位为秒
static const int HW_MAX_OUTPUT_LEN_DEFAULT = 1024;   //最大输出长度的默认值，单位为字节
static const int HW_MAX_OUTPUT_LEN_MIN = 1;          //最大输出长度的最小值，单位为字节
static const int HW_MAX_OUTPUT_LEN_MAX = 65536;      //最大输出长度的最大值，单位为字节

static const string HW_DB_HOST_DEFAULT = "10.80.42.244";																			//数据库服务器默认地址
static const int HW_DB_PORT_DEFAULT = 3306;																						  //数据库服务器默认端口
static const string HW_DB_NAME_DEFAULT = "homework";																			 //数据库默认名称
static const string HW_DB_USERNAME_DEFAULT = "hwapp_oop";																			//数据库默认用户名
static const string HW_DB_PASSWD_DEFAULT = "hwapp_2025*OoP-12*18";																	//数据库默认密码
static const string HW_DB_CURR_TERM_DEFAULT = "2025/2026/1";																		//数据库默认当前学期
static const string HW_DB_CNO_LIST_DEFAULT = "5000722002101,5000722002102,5000722002103,5000722002104,5000244014801,5000244001602"; //数据库默认课号列表

static const int SEP_LINE_LEN = 100; //分隔线长度

/*****************************************
	全局枚举定义
******************************************/
enum class HW_EXE_STYLE {
	single = 0,			     //学生的exe放在一个目录下（例：前面截图中exec-vs-3-b3-2.cpp-by-win）
	multi,				     //学生的exe放在各人的vs-exec/dev-exec下 
	none,					 //只测试参考程序运行是否正确，不检查学生作业 
	invalid					 //用于标记非法值
};

enum class HW_CMD_STYLE {
	normal = 0,              //正常方式，不带参数，不需要输入，例：九九乘法表 
	pipe,                    //管道方式输入数据，即get_input_data工具所用形式 
	redirection,             //重定向方式输入，即demo.exe < a.dat 形式
	main_with_arguments,	 //命令行带参数形式，例：部分oop作业
	invalid                  //用于标记非法值
};

/*
databse   ：要检查的学生列表从数据库中取，demo目前只支持按课号选取，在[数据库]
			组的db_cno_list 中给出要查询的课号即可（有效课号共6个，可单个/多
			个，用逗号分隔，未做容错处理）
非database：任意非database均当做文件名处理，打开该文件读取学生列表信息
			- 文件名为全路径文件名，命名规则不限，只要能正确读取即可
			- 文件要求GB编码，Linux/Windows格式均可
			- 每行三项，中间用空格/tab分隔，依次为课号、学号、姓名
*/

enum class HW_NAMELIST_MODE {
	database = 0,			 //学生信息从数据库中取
	file,					 //带全路径的学生列表文件名，格式为GB编码的纯txt文本
	invalid					 //用于标记非法值
};

/*****************************************
	全局结构体定义
******************************************/

/* tc系列参数：共8个，含义及取值范围均与txt_compare作业的对应参数相同 */

struct HW_DATABASE_CFG {
	HW_DATABASE_CFG();
	string db_host;
	int db_port;
	string db_name;
	string db_username;
	string db_passwd;
	string db_curr_term;
	string db_cno_list;  //逗号分隔的课号列表
};

struct HW_TC_CFG {
	HW_TC_CFG();
	string tc_trim;					//none/left/right/all
	int tc_lineskip;				//0~100
	int tc_lineoffset;				//-100~100
	int tc_ignoreblank;				//0/1
	int tc_not_ignore_linefeed;		//0/1
	int tc_maxdiff;					//0~100
	int tc_maxline;					//0~10000
	string tc_display;				//none/normal/detailed
};

struct HW_ITEMS_CFG {
	HW_ITEMS_CFG();
	int items_num;		//指定测试项的总数（取值[1..1024]) 
	int items_begin;	//指定测试的起始项数([1..items_num]) 
	int items_end;		//指定测试的结束项数([items_begin..items_num])

	//下面三个按 cmd_style 决定是否需要
	vector<string> item_gname;  //pipe 用：item_gname_1..n
	vector<string> item_fname;  //redirection 用：item_fname_1..n
	vector<string> item_args;   //main_with_arguments 用：item_args_1..n
};

struct HW_GROUP_CFG {
	HW_GROUP_CFG();
	//基本信息
	string group_name;    //例如[3-b3]
	string include_name;  //include = xxx（可能为空）

	HW_DATABASE_CFG database; //数据库相关参数

	//exe相关
	HW_EXE_STYLE exe_style;			//取值为single/multi/none三者之一
	string exe_style_str;           //保存原始字符串形式，便于打印
	string single_exe_dirname;		//指定 single 方式下存放所有学生exe的目录名 （例：D:\25261-term\exec-vs-3-b3-2.cpp-by-win）
	string multi_exe_main_dirname;  //指定根目录（例：D:\25261-term\allfile）
	string multi_exe_sub_dirname;   //指定学生目录下的exe文件目录（例：vs-exec）
	string stu_exe_name;			//指定学生exe文件名（例：3-b3-2.cpp.vs.x86.debug.exe）！ 完整的全路径文件名需要根据不同情况（single/multi）拼接
	string demo_exe_name;			//指定参考程序的全路径文件名 （例：D:\25261-term\参考exe\3-b3-demo-浮点数分解.exe） 

	//学生名单
	HW_NAMELIST_MODE name_list_mode; //取值为database/非database两者之一
	string name_list;				  //database 或 文件全路径

	//输入方式相关
	HW_CMD_STYLE cmd_style;					//取值为normal/pipe/redirection/main_with_argument 四种之一
	string cmd_style_str;				    //保存原始字符串形式，便于打印
	string pipe_get_input_data_exe_name;    //指定 get_input_data.exe 的全路径文件名
	string pipe_data_file;					//指定get_input_data.exe 所用的数据文件的全路径文件名
	string redirection_data_dirname;		//指定重定向方式输入的数据文件所在的目录

	//保护参数
	int timeout;		//设定超时时间，防止死循环（单位：秒，合理范围1-3600）
	int max_output_len; //设置最大输出长度，防止无效运行（单位：字节，合理范围1-65536）（例：求exp，读取的输入超过20字节则必定出错，直接终止程序运行）

	//tc系列参数
	HW_TC_CFG tc;	    //共8个参数，含义及取值范围均与txt_compare作业的对应参数相同

	//items 相关
	HW_ITEMS_CFG items; //测试项系列参数
};

struct HW_STUDENT_INFO {
	string class_no;      //课号
	string student_no;    //学号
	string name;		  //姓名
};


class hw_checker {
private:
	//基础参数
	string cfg_file;   //配置文件路径
	string check_name; //检查组名称
	string debug_type; //warn/info/debug/trace

	//解析后的配置内容
	HW_GROUP_CFG config;

	//检查配置文件的结果
	int checkcfg_ret; //0表示成功，非0表示失败

	//存放配置打印输出
	vector<string> cfg_used;
	ostringstream messages;
	ostringstream errors;

	//学生名单
	vector<HW_STUDENT_INFO> student_list;

	//demo输出
	vector<string> demo_outputs;
	vector<vector<unsigned char>> stu_pass_01;
	vector<vector<CheckExec_Errno>> stu_eno;
	vector<bool> stu_has_exe;
	string start_time_str;
	string end_time_str;

	//结果文件全路径
	string result_xls_fullpath;

	//成员函数

	//读取配置文件（每一组）
	void read_config_group(const string& group, HW_GROUP_CFG& config, 
		config_file_tools& Cfg_tool, vector<string> groups, bool is_base_group);
	//检查配置文件是否合法
	void check_config();
	//打印配置信息
	void store_cfg_info(const string& group);

	//验证配置项取值合法性及解析
	bool is_valid_exe_style(const string& s);
	bool is_valid_cmd_style(const string& s);
	HW_EXE_STYLE parse_exe_style(const string& s);
	HW_CMD_STYLE parse_cmd_style(const string& s);
	bool is_valid_cno(const string& cno);
	bool is_valid_sno(const string& sno);
	bool is_valid_trim(const string& s);
	bool is_valid_display(const string& s);

	//运行单个测试项，获取输出
	int run_one_case(const string& exe_fullpath, int item_index, string& out_text, CheckExec_Errno& out_eno);
	//构建执行命令行
	string build_exec_cmd(const string& exe_fullpath, int item_index);
	string find_student_exe_fullpath(const HW_STUDENT_INFO& stu);
	//获取测试项列标题
	string get_item_col_header(int item_index);
public:
	hw_checker(const string& _cfg_file, const string& _check_name, const string& _debug_type);
	~hw_checker();
	//加载配置文件
	void load_config();

	//打印配置文件摘要信息
	void print_cfg_info();
	void print_errors();
	int get_checkcfg_ret() const;

	//获取学生名单
	int load_student_list();
	bool is_student_list_empty() const;

	//正式比对
	int get_demo_outputs();
	int check_all_students();

	//结果保存
	void save_result_xls();
};
