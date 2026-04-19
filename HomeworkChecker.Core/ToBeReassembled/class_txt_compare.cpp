/* 2452769 幸可函 计科 */
#include "../include/class_txt_compare.h"
using namespace std;
/***************************************************************************
  函数名称：
  功    能：构造函数
  输入参数：
  返 回 值：
  说    明：文件名方式构造函数
  ***************************************************************************/
txt_compare::txt_compare(const string& fname1, const string& fname2, const string& trimtype, const string& displaytype,
	const int lineskip, const int lineoffset, const int linemaxdiffnum, const int linemaxlinenum,
	const bool ignoreblank, const bool crcrlfnotequal, const bool debugflag)
{
	p_stream1 = nullptr;
	p_stream2 = nullptr;
	use_iss = false;

	filename1 = fname1;
	filename2 = fname2;
	trim_type = trimtype;
	display_type = displaytype;
	line_skip = lineskip;
	line_offset = lineoffset;
	line_max_diffnum = linemaxdiffnum;
	line_max_linenum = linemaxlinenum;
	ignore_blank = ignoreblank;
	not_ignore_linefeed = crcrlfnotequal;
	debug = debugflag;
	line_maxlen = 0;
	diff_line_count = 0;
	open_files_success = false;
}

/***************************************************************************
  函数名称：
  功    能：构造函数
  输入参数：
  返 回 值：
  说    明：istringstream方式构造函数
  ***************************************************************************/
txt_compare::txt_compare(istringstream& iss1, istringstream& iss2, const string& trimtype, const string& displaytype,
	const int lineskip, const int lineoffset, const int linemaxdiffnum, const int linemaxlinenum,
	const bool ignoreblank, const bool crcrlfnotequal, const bool debugflag)
{
	p_stream1 = &iss1;
	p_stream2 = &iss2;
	use_iss = true;

	filename1 = "";
	filename2 = "";
	trim_type = trimtype;
	display_type = displaytype;
	line_skip = lineskip;
	line_offset = lineoffset;
	line_max_diffnum = linemaxdiffnum;
	line_max_linenum = linemaxlinenum;
	ignore_blank = ignoreblank;
	not_ignore_linefeed = crcrlfnotequal;
	debug = debugflag;
	line_maxlen = 0;
	diff_line_count = 0;
	open_files_success = false;
}

/***************************************************************************
  函数名称：
  功    能：析构函数
  输入参数：
  返 回 值：
  说    明：
 ***************************************************************************/
txt_compare::~txt_compare()
{
	/* 无动态内存，无需释放 */
}

/* ---------------------------------------------------------------
	 以下为私有成员函数的实现
---------------------------------------------------------------- */

/***************************************************************************
  函数名称：
  功    能：检查文件每行长度是否超过MAX_LINE_LENGTH
  输入参数：const string& filename - 文件名
  返 回 值：true - 通过检查，false - 有超长行
  说    明：第一次扫描文件，检查是否有超过MAX_LINE_LENGTH的行，同时更新line_maxlen
 ***************************************************************************/
void txt_compare::get_line_maxlen(const string& filename, FileStatus& fs)
{
	ifstream file(filename);

	while (!fs.is_eof)
	{
		read_next_line(file, fs);
		trim_line(fs.line);

		int end_len = 0;
		switch (fs.endtype) {
			case END_CR:
				end_len = 1;
				break;
			case END_LF:
				end_len = 1;
				break;
			case END_CRLF:
				end_len = 2;
				break;
			case END_EOF:
			default:
				break;
		}

		if ((int)fs.line.length() > this->line_maxlen)
		{
			this->line_maxlen = fs.line.length();
		}
	}

	file.close();
}


/***************************************************************************
  函数名称：
  功    能：对一行进行trim处理
  输入参数：string& line - 待处理的行
  返 回 值：
  说    明：
 ***************************************************************************/
void txt_compare::trim_line(string& line) const
{
	if (this->trim_type == "left" || this->trim_type == "all")
	{
		if (is_blank_line(line, true))
		{
			csu_trimLeft(line, false);
		}
		else
		{
			csu_trimLeft(line, true);
		}
	}
	if (this->trim_type == "right" || this->trim_type == "all")
	{
		csu_trimRight(line, true);
	}
}

/***************************************************************************
  函数名称：
  功    能：判断一行是否为空行
  输入参数：const string& line - 待检查的行
			const bool before_trim - 是否在trim前检查
  返 回 值：true - 是空行，false - 非空行
  说    明：
 ***************************************************************************/
bool txt_compare::is_blank_line(const string& line, const bool before_trim) const
{
	if (!before_trim)
	{
		return line.empty();
	}
	else
	{
		string temp = line;
		csu_trimAll(temp, true);
		return temp.empty();
	}
}

/***************************************************************************
  函数名称：
  功    能：跳过指定行数
  输入参数：ifstream& file - 文件流
			FileStatus& fs - 文件状态
			const int skipcount - 跳过行数
  返 回 值：
  说    明：
 ***************************************************************************/
void txt_compare::skip_line(istream& file, FileStatus& fs, const int skipcount)
{
	for (int i = 0; i < skipcount; i++)
	{
		read_next_line(file, fs);
		trim_line(fs.line);
		//忽略空行，不计入跳过行数
		if (this->ignore_blank && is_blank_line(fs.line)) {
			i--;
			continue;
		}
	}
}

/***************************************************************************
  函数名称：
  功    能：打印分隔线
  输入参数：
  返 回 值：
  说    明：
 ***************************************************************************/
void txt_compare::print_separator_line()
{
	int width = (this->line_maxlen / 10 + 1) * 10 + 8 + 2; //比---的标尺多2个：line_maxlen向上取10倍整数，8是"文件1 : "

	/* 如果加了hex输出，则最小宽度为80 */
	if (this->display_type == "detailed" && width < 80)
		width = 80;

	for (int i = 0; i < width; i++) {
		output_msg << "=";
	}
	output_msg << endl;
}

/***************************************************************************
  函数名称：
  功    能：打印阅读提示
  输入参数：
  返 回 值：
  说    明：
 ***************************************************************************/
void txt_compare::print_reading_tips()
{
	output_msg << "阅读提示：" << endl;
	output_msg << "\t1、每行的行结束符用<CR>/<LF>/<CR><LF>/<EOF>标出(方便看清行结束符的类型)" << endl;
	output_msg << "\t2、如果每行仅有<CR>/<LF>/<CR><LF>/<EOF>，则表示空行" << endl;
	output_msg << "\t3、文件结束标记为<EOF>" << endl;
	output_msg << "\t4、两行相同列位置的差异字符用亮色标出" << endl;
	output_msg << "\t5、每行中的CR/LF/VT/BS/BEL用X标出(方便看清隐含字符)" << endl;
	output_msg << "\t6、每行尾的多余的字符用亮色标出，VT/BS/BEL用亮色X标出(方便看清隐含字符)" << endl;
	output_msg << "\t7、中文因为编码问题，差异位置可能报在后半个汉字上，但整个汉字都亮色标出" << endl;

	//如果是normal模式，提示可以用detailed获得更多信息
	if (this->display_type == "normal") {
		output_msg << "\t8、用--display detailed可以得到更详细的信息" << endl;
	}
}

/***************************************************************************
  函数名称：
  功    能：打印标尺
  输入参数：const int maxlen - 当前行的最大长度
  返 回 值：
  说    明：
 ***************************************************************************/
void txt_compare::print_ruler(const int maxlen)
{
	int max_len = (maxlen / 10 + 2) * 10 + 1;

	output_msg << "        ";
	for (int i = 0; i < max_len; i++) {
		output_msg << "-";
	}
	output_msg << endl;

	output_msg << "        ";
	for (int i = 0; i <= max_len / 10; i++) {
		output_msg << char(i % 10 + '0') << "         ";
	}
	output_msg << endl;

	output_msg << "        ";
	for (int i = 0; i < max_len; i++) {
		output_msg << char(i % 10 + '0');
	}
	output_msg << endl;

	output_msg << "        ";
	for (int i = 0; i < max_len; i++) {
		output_msg << "-";
	}
	output_msg << endl;
}

/***************************************************************************
  函数名称：
  功    能：打印十六进制转储
  输入参数：const FileStatus& fs - 文件状态
  返 回 值：
  说    明：
 ***************************************************************************/
void txt_compare::print_hex_dump(const FileStatus& fs)
{
	this->output_msg << csu_StrtoHexdump(fs.line, fs.endtype);
}


/***************************************************************************
  函数名称：
  功    能：打印差异行的信息
  输入参数：const FileStatus& fs1 - 文件1的状态
			const FileStatus& fs2 - 文件2的状态
			bool same_content - 两行内容是否相同
			bool same_endtype - 两行结束符类型是否相同
  返 回 值：
  说    明：
 ***************************************************************************/
void txt_compare::print_diff_line(const FileStatus& fs1, const FileStatus& fs2, bool same_content, bool same_endtype, bool is_first_diff)
{
	bool has_eof = (fs1.is_eof || fs2.is_eof);

	// none 模式下不输出任何行级信息，但需要统计 diff_line_count
	if (this->display_type == "none") {
		return;
	}

	//2. 逐字符比较，算 first_diff_pos 等（尾部多余字符/一般差异都要用）
	string line1 = fs1.line;
	string line2 = fs2.line;

	const char* c_line1 = line1.c_str();
	const char* c_line2 = line2.c_str();
	vector<bool> is_diff_1;
	vector<bool> is_diff_2;
	int first_diff_pos = -1;
	int len1 = (int)line1.length();
	int len2 = (int)line2.length();

	int maxlen = (len1 > len2) ? len1 : len2;
	for (int i = 0; i < maxlen; i++) {
		bool diff = false;
		char ch1 = (i < len1) ? c_line1[i] : '\0';
		char ch2 = (i < len2) ? c_line2[i] : '\0';
		if (ch1 != ch2) {
			diff = true;
			if (first_diff_pos == -1) {
				first_diff_pos = i;
			}
		}
		is_diff_1.push_back(diff);
		is_diff_2.push_back(diff);
	}

	this->output_msg << "第[" << fs1.linenum << " / " << fs2.linenum << "]行 - ";


	if (same_content && !same_endtype)
	{
		this->output_msg << "行结束符不同" << endl;
	}
	else if (fs1.is_eof && !fs2.is_eof)
	{
		// 新版：有内容 or 后续行 => 统一用“文件1已结束/文件2仍有内容”
		this->output_msg << "文件1已结束/文件2仍有内容" << endl;
	}
	else if (!fs1.is_eof && fs2.is_eof)
	{
		this->output_msg << "文件2已结束/文件1仍有内容" << endl;
	}
	// 两边都未EOF：处理“某一行是另一行的前缀/空行 vs 有内容” => “文件x有多余字符”
	else if (first_diff_pos == len1 && len1 < len2)
	{
		this->output_msg << "文件2有多余字符" << endl;
	}
	else if (first_diff_pos == len2 && len2 < len1) {
		this->output_msg << "文件1有多余字符" << endl;
	}
	else
	{
		// 普通的“第[i]个字符开始有差异”
		if (first_diff_pos < 0)
			first_diff_pos = 0;
		this->output_msg << "第[" << first_diff_pos << "]个字符开始有差异" << endl;
	}


	//detailed模式要打印标尺
	if (this->display_type == "detailed" || this->display_type == "normal") {
		print_ruler(maxlen);
	}

	// 文件1
	this->output_msg << "文件1 : ";

	for (int i = 0; i < len1; i++) {
		if (is_diff_1[i])
			this->output_msg << HIGHLIGHT_START;
		char ch = c_line1[i];
		if (ch == '\r' || ch == '\n' || ch == '\v' || ch == '\b' || ch == '\a')
		{
			this->output_msg << 'X';
		}
		else
		{
			this->output_msg << ch;
		}
		if (is_diff_1[i])
			this->output_msg << HIGHLIGHT_END;
	}
	switch (fs1.endtype)
	{
		case END_CR:
			this->output_msg << "<CR>";
			break;
		case END_LF:
			this->output_msg << "<LF>";
			break;
		case END_CRLF:
			this->output_msg << "<CR><LF>";
			break;
		case END_EOF:
			this->output_msg << "<EOF>";
			break;
		default:
			break;
	}

	this->output_msg << endl;

	// 文件2
	this->output_msg << "文件2 : ";

	for (int i = 0; i < len2; i++)
	{
		if (is_diff_2[i]) {
			this->output_msg << HIGHLIGHT_START;
		}
		char ch = c_line2[i];
		if (ch == '\r' || ch == '\n' || ch == '\v' || ch == '\b' || ch == '\a')
		{
			this->output_msg << 'X';
		}
		else
		{
			this->output_msg << ch;
		}
		if (is_diff_2[i])
		{
			this->output_msg << HIGHLIGHT_END;
		}
	}

	switch (fs2.endtype)
	{
		case END_CR:
			this->output_msg << "<CR>";
			break;
		case END_LF:
			this->output_msg << "<LF>";
			break;
		case END_CRLF:
			this->output_msg << "<CR><LF>";
			break;
		case END_EOF:
			this->output_msg << "<EOF>";
			break;
		default:
			break;
	}

	this->output_msg << endl;

	if (this->display_type == "detailed") {
		this->output_msg << "文件1(HEX) : " << endl;
		print_hex_dump(fs1);
		this->output_msg << "文件2(HEX) : " << endl;
		print_hex_dump(fs2);
	}

	this->output_msg << endl;
}

/***************************************************************************
  函数名称：
  功    能：读取文件的下一行
  输入参数：ifstream& file - 文件流
			FileStatus& fs - 文件状态
  返 回 值：
  说    明：
 ***************************************************************************/
void txt_compare::read_next_line(istream& file, FileStatus& fs)
{
	fs.line.clear();
	fs.linenum++;
	int c = file.peek();
	if (c == EOF) {
		fs.is_eof = true;
		fs.endtype = END_EOF;
		return;
	}
	while (file.peek() != EOF) {
		c = file.get();
		if (c == '\r') {
			int next = file.peek();
			if (next == '\n') {
				file.get();
				fs.endtype = END_CRLF;
				return;
			}
			else if (next == EOF) {
				fs.endtype = END_CR;
				return;
			}
		}
		else if (c == '\n') {
			fs.endtype = END_LF;
			return;
		}
		else {
			fs.line += c;
		}
	}
	//到达文件末尾
	fs.is_eof = true;
	fs.endtype = END_EOF;
	return;
}


/* ---------------------------------------------------------------
	 以下为公有成员函数的实现
---------------------------------------------------------------- */

/***************************************************************************
  函数名称：
  功    能：执行比较操作
  输入参数：
  返 回 值：
  说    明：
 ***************************************************************************/
int txt_compare::compare(const bool silent)
{
	istream* in1 = this->p_stream1;
	istream* in2 = this->p_stream2;

	ifstream file1;
	ifstream file2;
	//1. 如果没有传入流，则按旧版本方式打开文件
	if (in1 == nullptr || in2 == nullptr) {
		if (this->filename1 == this->filename2)
		{
			cerr << "[--严重错误--] --file1 和 --file2 指定的文件名 [" << this->filename1 << "] 相同." << endl << endl << endl;
			return -1;
		}
		file1.open(this->filename1, ios::binary);
		if (!file1)
		{
			cerr << "[--严重错误--] 文件[" << this->filename1 << "]无法打开." << endl << endl << endl;
			return -1;
		}
		file2.open(this->filename2, ios::binary);
		if (!file2)
		{
			cerr << "[--严重错误--] 文件[" << this->filename2 << "]无法打开." << endl << endl << endl;
			return -1;
		}
		in1 = &file1;
		in2 = &file2;
	}


	this->open_files_success = true;
	/*比较需要用到的相关变量*/
	FileStatus fs1, fs2;

	int compared_lines = 0; //已比较的行数
	//2. 更新 line_maxlen
	if (!silent && !use_iss) {
		get_line_maxlen(this->filename1, fs1);
		get_line_maxlen(this->filename2, fs2);

		fs1 = FileStatus(); //重置
		fs2 = FileStatus(); //重置
	}


	//3. line_offset和line_skip预处理

	/*lineoffset（优先级：在lineskip之前）*/
	if (this->line_offset < 0) {
		//负数表示忽略file1的前n行
		skip_line(*in1, fs1, -this->line_offset);
	}
	else if (this->line_offset > 0) {
		//正数表示忽略file2的前n行
		skip_line(*in2, fs2, this->line_offset);
	}
	/*lineskip: 表示同时跳过两个文件的前s行*/
	skip_line(*in1, fs1, this->line_skip);
	skip_line(*in2, fs2, this->line_skip);
	if (!silent && !use_iss) {
		if (this->display_type != "none") {
			this->output_msg << "比较结果输出：" << endl;
			print_separator_line();
		}
	}
	//4. 开始逐行比较
	while (true)
	{
		//1.读取下一行（会读入'\r')
		read_next_line(*in1, fs1);
		read_next_line(*in2, fs2);

		//2. trim处理
		trim_line(fs1.line);
		trim_line(fs2.line);
		//3. 忽略空行处理
		if (this->ignore_blank) {
			while (!fs1.is_eof && is_blank_line(fs1.line)) {
				read_next_line(*in1, fs1);
				trim_line(fs1.line);
			}
			while (!fs2.is_eof && is_blank_line(fs2.line)) {
				read_next_line(*in2, fs2);
				trim_line(fs2.line);
			}
		}

		//4. 比较
		compared_lines++;
		/*达到最大差异行数限制*/
		if (this->line_max_linenum > 0 && compared_lines > this->line_max_linenum)
			break;

		bool same_content = (fs1.line == fs2.line);
		bool same_endtype = false;
		if (this->not_ignore_linefeed)
		{
			same_endtype = fs1.endtype == fs2.endtype;
		}
		else
		{
			same_endtype = (fs1.endtype == fs2.endtype) ||
				(fs1.endtype == END_CRLF && fs2.endtype == END_LF) ||
				(fs1.endtype == END_LF && fs2.endtype == END_CRLF) ||
				(fs1.endtype == END_CRLF && fs2.endtype == END_CR) ||
				(fs1.endtype == END_CR && fs2.endtype == END_CRLF) ||
				(fs1.endtype == END_LF && fs2.endtype == END_CR) ||
				(fs1.endtype == END_CR && fs2.endtype == END_LF);
		}

		//1.两边都 EOF	
		if (fs1.is_eof && fs2.is_eof) {
			if (!same_content || !same_endtype) {
				//只要最后一行不是“双方都空行”，就交给 print_diff_line 解释
				bool is_first_diff = (this->diff_line_count == 0);
				this->diff_line_count++;

				if (!silent) {
					print_diff_line(fs1, fs2, same_content, same_endtype, is_first_diff);
				}
			}
			break;
		}

		//2.只有一边 EOF
		if ((fs1.is_eof && !fs2.is_eof) || (!fs1.is_eof && fs2.is_eof)) {
			bool is_first_diff = (this->diff_line_count == 0);
			this->diff_line_count++;

			if (!silent) {
				print_diff_line(fs1, fs2, same_content, same_endtype, is_first_diff);
			}
			break;
		}

		//3.普通行内差异
		if (!same_content || !same_endtype) {
			bool is_first_diff = (this->diff_line_count == 0);
			this->diff_line_count++;

			if (!silent) {
				print_diff_line(fs1, fs2, same_content, same_endtype, is_first_diff);
			}
		}
		//检查是否达到最大差异行数限制
		if (this->line_max_diffnum > 0 && this->diff_line_count >= this->line_max_diffnum) {
			break;
		}


	}
	//5.输出最终结果
	if (!silent) {
		if (this->display_type == "none") {
			this->output_msg << ((this->diff_line_count == 0) ? "文件相同." : "文件不同.") << endl;
		}
		else { //detailed 或 normal 模式
			if (this->diff_line_count == 0)
				this->output_msg << "在指定检查条件下完全一致." << endl;
			else {
				print_separator_line();
				this->output_msg << "在指定检查条件下共" << this->diff_line_count << "行有差异";
				if (this->line_max_diffnum > 0 && this->diff_line_count >= this->line_max_diffnum)
					this->output_msg << "[已到设定的最大差异值]" << endl;
				this->output_msg << "." << endl;
				print_reading_tips();
			}
			print_separator_line();
		}
	}
	if (this->p_stream1 == nullptr || this->p_stream2 == nullptr) {
		file2.close();
		file1.close();
	}
	return this->diff_line_count;
}

/***************************************************************************
  函数名称：
  功    能：输出比较结果
  输入参数：
  返 回 值：
  说    明：
 ***************************************************************************/
void txt_compare::result() const
{
	if (open_files_success == false)
	{
		//文件打开失败，直接返回
		return;
	}
	const string msg = this->output_msg.str();
	for (size_t i = 0; i < msg.length(); i++) {
		//检查是否遇到 HS 或 HE 标记
		if (msg.compare(i, 4, HIGHLIGHT_START) == 0) {
			cct_setcolor(COLOR_HYELLOW, COLOR_RED); //遇到标记，设置高亮颜色
			i += 3;
			continue;
		}

		if (msg.compare(i, 4, HIGHLIGHT_END) == 0) {
			cct_setcolor(COLOR_BLACK, COLOR_WHITE);
			i += 3;
			continue;
		}
		//普通字符
		cout.write(&msg[i], 1);
	}
	//恢复默认颜色
	cct_setcolor(COLOR_BLACK, COLOR_WHITE);
}