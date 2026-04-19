/* 2452769 计科 幸可函 */
#define _CRT_SECURE_NO_WARNINGS
#include <iostream>
/* 添加自己需要的头文件，注意限制 */
#include "../include/class_cft.h"
using namespace std;

/* 给出各种自定义函数的实现（已给出的内容不全） */

//如果默认值不等于预定义的缺省值，则将value设为默认值，返回1，否则返回0
template<typename T>
static int check_default_value(T& value, const T& default_value, const T& defined_default_value)
{
	if (default_value != defined_default_value)
	{
		value = default_value;
		return 1;
	}
	return 0;
}

/*行处理：
1、 取出一行后，先截断;或#或//开始的注释，再去除前后空格/tab，剩下为有效内容
2、 有效内容为空则直接忽略该行
3、 有效内容的第一个字符是[，最后一个字符是]，则认为是组名，否则认为是项，均称为有效行
4、 如果该行为组，则去除前后[]后再做一次去除前后空格/tab的操作，不为空则为有效组名，否
则理解为组名为空（等价于简单配置文件的组名）
5、 如果该行是项，则在有效内容中查找分隔符（如果有多个，则认为左起第一个是分隔符，其它
是值，例：“name =  张三 = 李四 ”，则“ 张三 = 李四 ”为值）
6、 将项以分隔符做截断，左侧是项名，右侧是项值，将项名和项值再做一次去除前后空格/tab的
操作，分别称为有效项名和有效项值（例：“name =  张三 = 李四 ”，则“ 张三 = 李四 ”为
值，“张三 = 李四”为有效项值）；有效项值可能为空（例：“name = ”，另：目前这种去除前
后空格/tab的机制，不可能出现有效行第一个字符为空格/tab的情况）
7、 项值允许含空格、转义符、单双引号等，为了简化，均不做特殊处理，都当做普通字符
8、 从项值中取整数、浮点数、单字符、字符串、IP地址等各种数据类型时，统一的处理方式为将
值放入istringstream 中，再>>方式提取第一个有效值，fail()为 0 时取到的数据可信，否则
不可信
9、 如果某行的有效内容非空，不是组，也没有分隔符，则也当做项，但仅能被get_item_all按原
始方式全部读取时被读取
10、 约定一个配置文件只支持一种分隔符方式，在初始化时确定，如果有其它分隔符，可按原始
内容读出后自行进行后续处理 */

/* private函数部分 */

string config_file_tools::remove_comment(const string& line)
{
	int len = line.length();
	if (len == 0)
	{
		return line;
	}

	int pos = len; //注释的位置

	//查找 ';'
	for (int i = 0; i < len; i++)
	{
		if (line[i] == ';')
		{
			pos = i;
			break;
		}
	}
	//查找 '#'
	for (int i = 0; i < len; i++)
	{
		if (line[i] == '#')
		{
			if (i < pos)
			{
				pos = i;
			}
			break;
		}
	}
	//查找 '//'
	for (int i = 0; i < len - 1; i++)
	{
		if (line[i] == '/' && line[i + 1] == '/')
		{
			if (i < pos)
			{
				pos = i;
			}
			break;
		}
	}
	return line.substr(0, pos);
}

void config_file_tools::trim(string& str)
{
	csu_trimAll(str, true);
}

bool config_file_tools::is_group(const string& str)
{
	int len = str.length();
	//长度小于2，必然不是组
	if (len < 2)
	{
		return false;
	}
	//第一个字符必须是'['，最后一个字符必须是']'
	if (str[0] != '[')
	{
		return false;
	}
	if (str[len - 1] != ']')
	{
		return false;
	}

	return true;
}

string config_file_tools::get_valid_group_name(const string& str)
{
	int len = str.length();
	//去掉前后的[]
	string group_name = str.substr(1, len - 2);
	//去掉前后的空格和tab
	trim(group_name);
	if(group_name.empty())
	{
		return "";//组名为空，等价于简单配置文件的默认组
	}
	return ("[" + group_name + "]");//以[***]的形式存储
}

void config_file_tools::parse_item_line(const string& valid_line, cfg_item& item)
{
	int len = valid_line.length();
	int pos = -1;
	//保存原始行
	item.raw_line = valid_line;
	//查找分隔符位置
	if (item_separate_character_type == BREAK_CTYPE::Equal) //分隔符类型为 Equal（等号）
	{
		//查找 '='
		for (int i = 0; i < len; i++)
		{
			if (valid_line[i] == '=')
			{
				pos = i;
				break;
			}
		}
	}
	else //分隔符类型为 Space（空格/tab）
	{
		// 查找第一个空格或tab
		for (int i = 0; i < len; i++)
		{
			if (valid_line[i] == ' ' || valid_line[i] == '\t')
			{
				pos = i;
				break;
			}
		}
	}
	//没找到分隔符，则有效项名和有效项值均为空
	if (pos == -1)
	{
		item.item_name = "";
		item.item_value = "";
		return;
	}
	//找到了分隔符，则截断
	string left = valid_line.substr(0, pos);
	string right = valid_line.substr(pos + 1, len);
	//去掉前后空格和tab
	trim(left);
	trim(right);
	//有效值为trim后的
	item.item_name = left;
	item.item_value = right;
}

int config_file_tools::find_group(const string& target_group_name, const bool is_case_sensitive) const
{
	for (size_t i = 0; i < groups.size(); i++)
	{
		if (!is_case_sensitive)
		{
			if (csu_toLower(groups[i].group_name) == csu_toLower(target_group_name))
			{
				return i;
			}
		}
		else
		{
			if (groups[i].group_name == target_group_name)
			{
				return i;
			}
		}
	}
	return -1;
}

int config_file_tools::find_item(const int group_index, const string& target_item_name, const bool is_case_sensitive) const
{
	if (group_index < 0 || (size_t)group_index >= groups.size())
	{
		return -1;
	}
	const vector<cfg_item>& items = groups[group_index].items;
	for (size_t i = 0; i < items.size(); i++)
	{
		if (!is_case_sensitive)
		{
			if (csu_toLower(items[i].item_name) == csu_toLower(target_item_name))
			{
				return i;
			}
		}
		else
		{
			if (items[i].item_name == target_item_name)
			{
				return i;
			}
		}
	}
	return -1;
}

/***************************************************************************
  函数名称：
  功    能：
  输入参数：
  返 回 值：
  说    明：构造函数，指定要读取的配置文件名及分隔符的形式
***************************************************************************/
config_file_tools::config_file_tools(const char* const _cfgname, const enum BREAK_CTYPE _ctype)
{
	//初始化
	cfgname = _cfgname;
	item_separate_character_type = _ctype;
	read_succeeded = false;
	groups.clear();
	//打开文件
	ifstream fin(cfgname);
	if (!fin)
	{
		return;
	}

	string line;
	int line_num = 0;
	string current_group_name; //当前组，初始为简单配置文件的默认组
	bool has_valid_line = false; //标记是否有有效行

	//不含分隔符的项名直接忽略即可（不读取，也不必报错） 
	while (getline(fin, line))
	{
		//一行长度超过最大限制，认为文件非法，读取失败，直接返回
		if (line.size() > MAX_LINE)
		{
			cout << "非法格式的配置文件，第[" << line_num << "]行超过最大长度" << MAX_LINE << "." << endl;
			return;
		}
		//去掉注释
		string valid_line = remove_comment(line);
		//去掉前后空格和tab
		trim(valid_line);
		//有效内容为空，忽略该行
		if (valid_line.empty())
		{
			continue;
		}
		else
		{
			has_valid_line = true;
		}
		//判断是否为组
		if (is_group(valid_line))
		{
			//取组名
			current_group_name = get_valid_group_name(valid_line);
			//添加新组
			int group_index = find_group(current_group_name);
			if (group_index == -1)
			{
				groups.push_back({ current_group_name, {} });
			}
		}
		else //该行是项
		{
			cfg_item item;
			//解析配置项行，取出项名、项值、是否含分隔符
			parse_item_line(valid_line, item);
			//判断是正常配置文件，还是简单/混合配置文件
			if (groups.size() == 0) //简单/混合配置文件，先添加默认组
			{
				groups.push_back({ SIMPLE_GNAME, {} });
			}

			int group_index = find_group(current_group_name);
			groups[group_index].items.push_back(item);

		}
	}
	//判断是否全空行或注释（没有有效行）
	if (!has_valid_line)
	{
		return;
	}
	//读取成功
	read_succeeded = true;
}



/***************************************************************************
  函数名称：
  功    能：
  输入参数：
  返 回 值：
  说    明：
***************************************************************************/
config_file_tools::config_file_tools(const string& _cfgname, const enum BREAK_CTYPE _ctype)
{
	//初始化
	cfgname = _cfgname;
	item_separate_character_type = _ctype;
	read_succeeded = false;
	groups.clear();
	//打开文件
	ifstream fin(cfgname);
	if (!fin)
	{
		return;
	}

	string line;
	int line_num = 0;
	string current_group_name; //当前组，初始为简单配置文件的默认组
	bool has_valid_line = false; //标记是否有有效行

	//不含分隔符的项名直接忽略即可（不读取，也不必报错） 
	while (getline(fin, line))
	{
		line_num++;
		//一行长度超过最大限制，认为文件非法，读取失败，直接返回
		if (line.size() > MAX_LINE)
		{
			cout << "非法格式的配置文件，第[" << line_num << "]行超过最大长度" << MAX_LINE << "." << endl;
			return;
		}
		//去掉注释
		string valid_line = remove_comment(line);
		//去掉前后空格和tab
		trim(valid_line);
		//有效内容为空，忽略该行
		if (valid_line.empty())
		{
			continue;
		}
		else
		{
			has_valid_line = true;
		}

		//判断是否为组
		if (is_group(valid_line))
		{
			//取组名
			current_group_name = get_valid_group_name(valid_line);
			//添加新组
			int group_index = find_group(current_group_name);
			if (group_index == -1)
			{
				groups.push_back({ current_group_name, {} });
			}
		}
		else //该行是项
		{
			cfg_item item;
			//解析配置项行，取出项名、项值、是否含分隔符
			parse_item_line(valid_line, item);
			//判断是正常配置文件，还是简单/混合配置文件
			if(groups.size() == 0) //简单/混合配置文件，先添加默认组
			{
				groups.push_back({ SIMPLE_GNAME, {} });
			}

			int group_index = find_group(current_group_name);
			groups[group_index].items.push_back(item);

		}
	}
	//判断是否全空行或注释（没有有效行）
	if (!has_valid_line)
	{
		return;
	}
	//读取成功
	read_succeeded = true;
}

/***************************************************************************
  函数名称：
  功    能：
  输入参数：
  返 回 值：
  说    明：
***************************************************************************/
config_file_tools::~config_file_tools()
{
	/* 按需完成 */
}


/***************************************************************************
  函数名称：
  功    能：判断读配置文件是否成功
  输入参数：
  返 回 值：true - 成功，已读入所有的组/项
		   false - 失败，文件某行超长/文件全部是注释语句
  说    明：
***************************************************************************/
bool config_file_tools::is_read_succeeded() const
{
	return read_succeeded;
}

/***************************************************************************
  函数名称：
  功    能：返回配置文件中的所有组
  输入参数：vector <string>& ret : vector 中每项为一个组名
  返 回 值：读到的组的数量（简单配置文件的组数量为1，组名为"）
  说    明：返回配置文件中的所有组，放在vector中
		   如果有多个group 相同（无论是连续出现/间隔出现），均当做一个组，同名组中的所有项目均合并到一个组下面，不去重
		   对于简单配置文件，返回一个空组（空组组名为""），返回值为1
		   对于混合配置文件，第一个是空组""，后续是有组名的组，返回值是非空组名数+1
***************************************************************************/
int config_file_tools::get_all_group(vector <string>& ret)
{
	ret.clear();
	for (size_t i = 0; i < groups.size(); i++)
	{
		ret.push_back(groups[i].group_name);
	}
	return groups.size();
}

/***************************************************************************
  函数名称：
  功    能：查找指定组的所有项并返回项的原始内容
  输入参数：const char* const group_name：组名
		   vector <string>& ret：vector 中每项为一个项的原始内容
		   const bool is_case_sensitive = false : 组名是否大小写敏感，true-大小写敏感 / 默认false-大小写不敏感
  返 回 值：项的数量，0表示空
  说    明：对于简单配置文件，group_name 指定为""
		返回的是该组的有效行的全部内容的完整字符串形式（不考虑值类型，供后续自行处理；
		全部有效内容指去除项目名之前/值之后的空格、tab等，下同），不考虑是否含分隔符，
		也不考虑项是否重复
***************************************************************************/
int config_file_tools::get_all_item(const char* const group_name, vector <string>& ret, const bool is_case_sensitive)
{
	ret.clear();

	if (group_name == NULL)
	{
		return 0;
	}
	string target_group_name = group_name;
	//查找组
	int group_index = find_group(target_group_name, is_case_sensitive);
	if (group_index != -1)
	{
		for (size_t i = 0; i < groups[group_index].items.size(); i++)
		{
			ret.push_back(groups[group_index].items[i].raw_line);
		}
	}
	return ret.size();
}

/***************************************************************************
  函数名称：
  功    能：组名/项目为string方式，其余同上
  输入参数：
  返 回 值：
  说    明：
***************************************************************************/
int config_file_tools::get_all_item(const string& group_name, vector <string>& ret, const bool is_case_sensitive)
{
	return this->get_all_item(group_name.c_str(), ret, is_case_sensitive);
}

/***************************************************************************
  函数名称：
  功    能：取某项的原始内容（=后的所有字符，string方式）
  输入参数：const char* const group_name
		   const char* const item_name
		   string &ret
		   const bool group_is_case_sensitive = false : 组名是否大小写敏感，true-大小写敏感 / 默认false-大小写不敏感
		   const bool item_is_case_sensitive = false  : 项名是否大小写敏感，true-大小写敏感 / 默认false-大小写不敏感
  返 回 值：
  说    明：
***************************************************************************/
int config_file_tools::item_get_raw(const char* const group_name, const char* const item_name, string& ret, const bool group_is_case_sensitive, const bool item_is_case_sensitive)
{
	ret.clear();
	//若组名为空，直接返回0
	if (group_name == NULL)
	{
		return 0;
	}
	//项名同理
	if (item_name == NULL)
	{
		return 0;
	}
	string target_group_name = group_name;
	string target_item_name = item_name;
	//查找组
	int group_index = find_group(target_group_name, group_is_case_sensitive);
	if (group_index == -1)
	{
		return 0;
	}
	//查找项
	int item_index = find_item(group_index, target_item_name, item_is_case_sensitive);
	if (item_index == -1)
	{
		return 0;
	}
	//查找成功，取值
	ret = groups[group_index].items[item_index].item_value;
	return 1;
}

/***************************************************************************
  函数名称：
  功    能：组名/项目为string方式，其余同上
  输入参数：
  返 回 值：
  说    明：
***************************************************************************/
int config_file_tools::item_get_raw(const string& group_name, const string& item_name, string& ret, const bool group_is_case_sensitive, const bool item_is_case_sensitive)
{
	/* 本函数已实现 */
	return this->item_get_raw(group_name.c_str(), item_name.c_str(), ret, group_is_case_sensitive, item_is_case_sensitive);
}

/***************************************************************************
  函数名称：
  功    能：
  输入参数：const char* const group_name               ：组名
		   const char* const item_name                ：项名
		   const bool group_is_case_sensitive = false : 组名是否大小写敏感，true-大小写敏感 / 默认false-大小写不敏感
		   const bool item_is_case_sensitive = false  : 项名是否大小写敏感，true-大小写敏感 / 默认false-大小写不敏感
  返 回 值：1 - 该项的项名存在
		   0 - 该项的项名不存在
  说    明：
***************************************************************************/
int config_file_tools::item_get_null(const char* const group_name, const char* const item_name, const bool group_is_case_sensitive, const bool item_is_case_sensitive)
{
	//若组名为空，直接返回0
	if (group_name == NULL)
	{
		return 0;
	}
	//项名同理
	if (item_name == NULL)
	{
		return 0;
	}
	string target_group_name = group_name;
	string target_item_name = item_name;
	//查找组
	int group_index = find_group(target_group_name, group_is_case_sensitive);
	if (group_index == -1)
	{
		return 0;
	}
	//查找项
	int item_index = find_item(group_index, target_item_name, item_is_case_sensitive);
	if (item_index == -1)
	{
		return 0;
	}
	//查找成功
	return 1;
}

/***************************************************************************
  函数名称：
  功    能：组名/项目为string方式，其余同上
  输入参数：
  返 回 值：
  说    明：因为工具函数一般在程序初始化阶段被调用，不会在程序执行中被高频次调用，
		   因此这里直接套壳，会略微影响效率，但不影响整体性能（对高频次调用，此方法不适合）
***************************************************************************/
int config_file_tools::item_get_null(const string& group_name, const string& item_name, const bool group_is_case_sensitive, const bool item_is_case_sensitive)
{
	/* 本函数已实现 */
	return this->item_get_null(group_name.c_str(), item_name.c_str(), group_is_case_sensitive, item_is_case_sensitive);
}

/***************************************************************************
  函数名称：
  功    能：取某项的内容，返回类型为char型
  输入参数：const char* const group_name               ：组名
		   const char* const item_name                ：项名
		   char& value                                ：读到的char的值（返回1时可信，返回0则不可信）
		   const char* const choice_set = nullptr     ：合法的char的集合（例如："YyNn"表示合法输入为Y/N且不分大小写，该参数有默认值nullptr，表示全部字符，即不检查）
		   const char def_value = DEFAULT_CHAR_VALUE  ：读不到/读到非法的情况下的默认值，该参数有默认值DEFAULT_CHAR_VALUE，分两种情况
															当值是   DEFAULT_CHAR_VALUE 时，返回0（值不可信）
															当值不是 DEFAULT_CHAR_VALUE 时，令value=def_value并返回1
		   const bool group_is_case_sensitive = false : 组名是否大小写敏感，true-大小写敏感 / 默认false-大小写不敏感
		   const bool item_is_case_sensitive = false  : 项名是否大小写敏感，true-大小写敏感 / 默认false-大小写不敏感
  返 回 值：1 - 取到正确值
			   未取到值/未取到正确值，设置了缺省值（包括设为缺省值）
		   0 - 未取到（只有为未指定默认值的情况下才会返回0）
  说    明：注意：取具体数据类型的函数，均按照【行处理逻辑：】第8条执行
***************************************************************************/
int config_file_tools::item_get_char(const char* const group_name, const char* const item_name, char& value,
	const char* const choice_set, const char def_value, const bool group_is_case_sensitive, const bool item_is_case_sensitive)
{
	string item_value;
	//取原始值
	if (!item_get_raw(group_name, item_name, item_value, group_is_case_sensitive, item_is_case_sensitive))
	{
		//取值失败
		return check_default_value(value ,def_value, DEFAULT_CHAR_VALUE);
	}
	//取值成功，放入istringstream中
	istringstream istr(item_value);
	char tmp;
	istr >> tmp;

	if (istr.fail())
	{
		//取值失败
		return check_default_value(value,def_value, DEFAULT_CHAR_VALUE);
	}
	//检查合法性
	if (choice_set != nullptr)
	{
		bool is_valid = false;
		for (int i = 0; choice_set[i] != '\0'; i++)
		{
			if (tmp == choice_set[i])
			{
				is_valid = true;
				break;
			}
		}
		if (!is_valid)
		{
			//取值失败
			return check_default_value(value,def_value, DEFAULT_CHAR_VALUE);
		}
	}
	//取值成功且合法
	value = tmp;
	return 1;
}

/***************************************************************************
  函数名称：
  功    能：组名/项目为string方式，其余同上
  输入参数：
  返 回 值：
  说    明：因为工具函数一般在程序初始化阶段被调用，不会在程序执行中被高频次调用，
		   因此这里直接套壳，会略微影响效率，但不影响整体性能（对高频次调用，此方法不适合）
***************************************************************************/
int config_file_tools::item_get_char(const string& group_name, const string& item_name, char& value,
	const char* const choice_set, const char def_value, const bool group_is_case_sensitive, const bool item_is_case_sensitive)
{
	/* 本函数已实现 */
	return this->item_get_char(group_name.c_str(), item_name.c_str(), value, choice_set, def_value, group_is_case_sensitive, item_is_case_sensitive);
}

/***************************************************************************
  函数名称：
  功    能：取某项的内容，返回类型为int型
  输入参数：const char* const group_name               ：组名
		   const char* const item_name                ：项名
		   int& value                                 ：读到的int的值（返回1时可信，返回0则不可信）
		   const int min_value = INT_MIN              : 期望数据范围的下限，默认为INT_MIN
		   const int max_value = INT_MAX              : 期望数据范围的上限，默认为INT_MAX
		   const int def_value = DEFAULT_INT_VALUE    ：读不到/读到非法的情况下的默认值，该参数有默认值 DEFAULT_INT_VALUE，分两种情况
															当值是   DEFAULT_INT_VALUE 时，返回0（值不可信）
															当值不是 DEFAULT_INT_VALUE 时，令value=def_value并返回1
		   const bool group_is_case_sensitive = false : 组名是否大小写敏感，true-大小写敏感 / 默认false-大小写不敏感
		   const bool item_is_case_sensitive = false  : 项名是否大小写敏感，true-大小写敏感 / 默认false-大小写不敏感
  返 回 值：
  说    明：
***************************************************************************/
int config_file_tools::item_get_int(const char* const group_name, const char* const item_name, int& value,
	const int min_value, const int max_value, const int def_value, const bool group_is_case_sensitive, const bool item_is_case_sensitive)
{
	string item_value;
	//取原始值
	if (!item_get_raw(group_name, item_name, item_value, group_is_case_sensitive, item_is_case_sensitive))
	{
		//取值失败
		return check_default_value(value,def_value, DEFAULT_INT_VALUE);
	}
	//取值成功，放入istringstream中
	istringstream istr(item_value);
	int tmp;
	istr >> tmp;

	if (istr.fail())
	{
		//取值失败
		return check_default_value(value,def_value, DEFAULT_INT_VALUE);
	}
	//检查合法性
	if (tmp < min_value || tmp > max_value)
	{
		//不合法
		return check_default_value(value,def_value, DEFAULT_INT_VALUE);
	}
	//取值成功且合法
	value = tmp;
	return 1;
}

/***************************************************************************
  函数名称：
  功    能：组名/项目为string方式，其余同上
  输入参数：
  返 回 值：
  说    明：因为工具函数一般在程序初始化阶段被调用，不会在程序执行中被高频次调用，
		   因此这里直接套壳，会略微影响效率，但不影响整体性能（对高频次调用，此方法不适合）
***************************************************************************/
int config_file_tools::item_get_int(const string& group_name, const string& item_name, int& value,
	const int min_value, const int max_value, const int def_value, const bool group_is_case_sensitive, const bool item_is_case_sensitive)
{
	/* 本函数已实现 */
	return this->item_get_int(group_name.c_str(), item_name.c_str(), value, min_value, max_value, def_value, group_is_case_sensitive, item_is_case_sensitive);
}

/***************************************************************************
  函数名称：
  功    能：取某项的内容，返回类型为double型
  输入参数：const char* const group_name                  ：组名
		   const char* const item_name                   ：项名
		   double& value                                 ：读到的int的值（返回1时可信，返回0则不可信）
		   const double min_value = __DBL_MIN__          : 期望数据范围的下限，默认为INT_MIN
		   const double max_value = __DBL_MAX__          : 期望数据范围的上限，默认为INT_MAX
		   const double def_value = DEFAULT_DOUBLE_VALUE ：读不到/读到非法的情况下的默认值，该参数有默认值DEFAULT_DOUBLE_VALUE，分两种情况
																当值是   DEFAULT_DOUBLE_VALUE 时，返回0（值不可信）
																当值不是 DEFAULT_DOUBLE_VALUE 时，令value=def_value并返回1
		   const bool group_is_case_sensitive = false     : 组名是否大小写敏感，true-大小写敏感 / 默认false-大小写不敏感
		   const bool item_is_case_sensitive = false      : 项名是否大小写敏感，true-大小写敏感 / 默认false-大小写不敏感
  返 回 值：
  说    明：
***************************************************************************/
int config_file_tools::item_get_double(const char* const group_name, const char* const item_name, double& value,
	const double min_value, const double max_value, const double def_value, const bool group_is_case_sensitive, const bool item_is_case_sensitive)
{
	string item_value;
	//取原始值
	if (!item_get_raw(group_name, item_name, item_value, group_is_case_sensitive, item_is_case_sensitive))
	{
		return check_default_value(value, def_value, DEFAULT_DOUBLE_VALUE);
	}
	//取值成功，放入istringstream中
	istringstream istr(item_value);
	double tmp;
	istr >> tmp;

	if (istr.fail())
	{
		//取值失败
		return check_default_value(value, def_value, DEFAULT_DOUBLE_VALUE);
	}
	//检查合法性
	if (tmp < min_value || tmp > max_value)
	{
		//不合法
		return check_default_value(value, def_value, DEFAULT_DOUBLE_VALUE);
	}
	//取值成功且合法
	value = tmp;
	return 1;
}

/***************************************************************************
  函数名称：
  功    能：组名/项目为string方式，其余同上
  输入参数：
  返 回 值：
  说    明：因为工具函数一般在程序初始化阶段被调用，不会在程序执行中被高频次调用，
		   因此这里直接套壳，会略微影响效率，但不影响整体性能（对高频次调用，此方法不适合）
***************************************************************************/
int config_file_tools::item_get_double(const string& group_name, const string& item_name, double& value,
	const double min_value, const double max_value, const double def_value, const bool group_is_case_sensitive, const bool item_is_case_sensitive)
{
	/* 本函数已实现 */
	return this->item_get_double(group_name.c_str(), item_name.c_str(), value, min_value, max_value, def_value, group_is_case_sensitive, item_is_case_sensitive);
}

/***************************************************************************
  函数名称：
  功    能：取某项的内容，返回类型为char * / char []型
  输入参数：const char* const group_name                  ：组名
		   const char* const item_name                   ：项名
		   char *const value                             ：读到的C方式的字符串（返回1时可信，返回0则不可信）
		   const int str_maxlen                          ：指定要读的最大长度（含尾零）
																如果<1则返回空串(不是DEFAULT_CSTRING_VALUE，虽然现在两者相同，但要考虑default值可能会改)
																如果>MAX_STRLEN 则上限为MAX_STRLEN
		   const char* const def_str                     ：读不到情况下的默认值，该参数有默认值DEFAULT_CSTRING_VALUE，分两种情况
																当值是   DEFAULT_CSTRING_VALUE 时，返回0（值不可信）
																当值不是 DEFAULT_CSTRING_VALUE 时，令value=def_value并返回1（注意，不是直接=）
		   const bool group_is_case_sensitive = false : 组名是否大小写敏感，true-大小写敏感 / 默认false-大小写不敏感
		   const bool item_is_case_sensitive = false  : 项名是否大小写敏感，true-大小写敏感 / 默认false-大小写不敏感
  返 回 值：
  说    明：1、为简化，未对\"等做转义处理，均按普通字符
		   2、含尾零的最大长度为str_maxlen，调用时要保证有足够空间
		   3、如果 str_maxlen 超过了系统预设的上限 MAX_STRLEN，则按 MAX_STRLEN 取
***************************************************************************/
int config_file_tools::item_get_cstring(const char* const group_name, const char* const item_name, char* const value,
	const int str_maxlen, const char* const def_value, const bool group_is_case_sensitive, const bool item_is_case_sensitive)
{
	if (str_maxlen < 1)
	{
		//如果<1则返回空串(不是DEFAULT_CSTRING_VALUE，
		value[0] = '\0';
		return 0;
	}
	int maxlen = (str_maxlen > MAX_STRLEN) ? MAX_STRLEN : str_maxlen;

	string item_value;
	//取原始值
	if (!item_get_raw(group_name, item_name, item_value, group_is_case_sensitive, item_is_case_sensitive))
	{
		//取值失败
		if (!strcmp(def_value, DEFAULT_CSTRING_VALUE))
		{
			value[0] = '\0';
			return 0;
		}
		else
		{
			//取默认值
			snprintf(value, maxlen, "%s", def_value);
			return 1;
		}
	}
	//取值成功
	istringstream istr(item_value);
	string tmp;
	istr >> tmp;

	if(istr.fail())
	{
		//取值失败
		if (!strcmp(def_value, DEFAULT_CSTRING_VALUE))
		{
			value[0] = '\0';
			return 0;
		}
		else
		{
			//取默认值
			snprintf(value, maxlen, "%s", def_value);
			return 1;
		}
	}
	//取值成功且合法
	snprintf(value, maxlen, "%s", tmp.c_str());

	return 1;
}

/***************************************************************************
  函数名称：
  功    能：组名/项目为string方式，其余同上
  输入参数：
  返 回 值：
  说    明：因为工具函数一般在程序初始化阶段被调用，不会在程序执行中被高频次调用，
		   因此这里直接套壳，会略微影响效率，但不影响整体性能（对高频次调用，此方法不适合）
***************************************************************************/
int config_file_tools::item_get_cstring(const string& group_name, const string& item_name, char* const value,
	const int str_maxlen, const char* const def_value, const bool group_is_case_sensitive, const bool item_is_case_sensitive)

{
	/* 本函数已实现 */
	return item_get_cstring(group_name.c_str(), item_name.c_str(), value, str_maxlen, def_value, group_is_case_sensitive, item_is_case_sensitive);
}

/***************************************************************************
  函数名称：
  功    能：取某项的内容，返回类型为 string 型
  输入参数：const char* const group_name               ：组名
		   const char* const item_name                ：项名
		   string &value                              ：读到的string方式的字符串（返回1时可信，返回0则不可信）
		   const string &def_value                    ：读不到情况下的默认值，该参数有默认值DEFAULT_STRING_VALUE，分两种情况
															当值是   DEFAULT_STRING_VALUE 时，返回0（值不可信）
															当值不是 DEFAULT_STRING_VALUE 时，令value=def_value并返回1
		   const bool group_is_case_sensitive = false : 组名是否大小写敏感，true-大小写敏感 / 默认false-大小写不敏感
		   const bool item_is_case_sensitive = false  : 项名是否大小写敏感，true-大小写敏感 / 默认false-大小写不敏感
  返 回 值：
  说    明：为简化，未对\"等做转义处理，均按普通字符
***************************************************************************/
int config_file_tools::item_get_string(const char* const group_name, const char* const item_name, string& value,
	const string& def_value, const bool group_is_case_sensitive, const bool item_is_case_sensitive)
{
	string item_value;
	//取原始值
	if (!item_get_raw(group_name, item_name, item_value, group_is_case_sensitive, item_is_case_sensitive))
	{
		//取值失败
		return check_default_value(value, def_value, DEFAULT_STRING_VALUE);
	}
	//取值成功
	istringstream istr(item_value);
	string tmp;
	istr >> tmp;
	if (istr.fail())
	{
		//取值失败
		return check_default_value(value, def_value, DEFAULT_STRING_VALUE);
	}
	//取值成功且合法
	value = tmp;
	return 1;
}

/***************************************************************************
  函数名称：
  功    能：组名/项目为string方式，其余同上
  输入参数：
  返 回 值：
  说    明：因为工具函数一般在程序初始化阶段被调用，不会在程序执行中被高频次调用，
		   因此这里直接套壳，会略微影响效率，但不影响整体性能（对高频次调用，此方法不适合）
***************************************************************************/
int config_file_tools::item_get_string(const string& group_name, const string& item_name, string& value,
	const string& def_value, const bool group_is_case_sensitive, const bool item_is_case_sensitive)
{
	/* 本函数已实现 */
	return this->item_get_string(group_name.c_str(), item_name.c_str(), value, def_value, group_is_case_sensitive, item_is_case_sensitive);
}

/***************************************************************************
  函数名称：
  功    能：取某项的内容，返回类型为 IPv4 地址的32bit整型（主机序）
  输入参数：const char* const group_name               ：组名
		   const char* const item_name                ：项名
		   unsigned int &value                        ：读到的IP地址，32位整型方式（返回1时可信，返回0则不可信）
		   const unsigned int &def_value              ：读不到情况下的默认值，该参数有默认值DEFAULT_IPADDR_VALUE，分两种情况
															当值是   DEFAULT_IPADDR_VALUE 时，返回0（值不可信）
															当值不是 DEFAULT_IPADDR_VALUE 时，令value=def_value并返回1
		   const bool group_is_case_sensitive = false : 组名是否大小写敏感，true-大小写敏感 / 默认false-大小写不敏感
		   const bool item_is_case_sensitive = false  : 项名是否大小写敏感，true-大小写敏感 / 默认false-大小写不敏感
  返 回 值：
  说    明：
***************************************************************************/
int config_file_tools::item_get_ipaddr(const char* const group_name, const char* const item_name, unsigned int& value,
	const unsigned int& def_value, const bool group_is_case_sensitive, const bool item_is_case_sensitive)
{
	string item_value;
	//取原始值
	if (!item_get_raw(group_name, item_name, item_value, group_is_case_sensitive, item_is_case_sensitive))
	{
		//取值失败
		return check_default_value(value, def_value, DEFAULT_IPADDR_VALUE);
	}
	//取值成功，放入istringstream中
	istringstream istr(item_value);
	string tmp;
	istr >> tmp;

	if(istr.fail())
	{
		//取值失败
		return check_default_value(value, def_value, DEFAULT_IPADDR_VALUE);
	}
	//检查合法性
	IpConversionResult result = csu_StrtoIpaddr(tmp);
	if (!result.ICR_valid)
	{
		//取值失败
		return check_default_value(value, def_value, DEFAULT_IPADDR_VALUE);
	}
	//取值成功且合法
	value = result.ICR_value;
	return 1;
}

/***************************************************************************
  函数名称：
  功    能：组名/项目为string方式，其余同上
  输入参数：
  返 回 值：
  说    明：因为工具函数一般在程序初始化阶段被调用，不会在程序执行中被高频次调用，
		   因此这里直接套壳，会略微影响效率，但不影响整体性能（对高频次调用，此方法不适合）
***************************************************************************/
int config_file_tools::item_get_ipaddr(const string& group_name, const string& item_name, unsigned int& value,
	const unsigned int& def_value, const bool group_is_case_sensitive, const bool item_is_case_sensitive)
{
	/* 本函数已实现 */
	return this->item_get_ipaddr(group_name.c_str(), item_name.c_str(), value, def_value, group_is_case_sensitive, item_is_case_sensitive);
}
