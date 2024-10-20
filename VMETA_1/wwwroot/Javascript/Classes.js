class Message {
    constructor(role, content, images = null, tool_calls = null) {
        this.role = role;
        this.content = content;
        this.images = images;
        this.tool_calls = tool_calls;
    }

    toJSON() {
        return {
            role: this.role,
            content: this.content,
            images: this.images,
            tool_calls: this.tool_calls,
        };
    }
}
class Person {
    constructor(name, surname, classroom, birth,email,cellphone,telegramCode,isStudent) {
        this.name = name;
        this.surname = surname;
        this.classroom = classroom;
        this.birth = birth;
        this.email = email;
        this.cellphone = cellphone;
        this.telegramCode = telegramCode;
        this.isStudent = isStudent;
    }

}
class Classroom {
    constructor(year, sect, spec) {

        this.year = year;
        this.section = sect;
        this.specialization = spec;

    }
}
class Problem {

    constructor(classroom, description, person, secret, solution, title, category,aiforced,id) {

        this.classroom = classroom;
        this.description = description;
        this.person = person;
        this.secret = secret;
        this.solution = solution;
        this.title = title;
        this.category = category;
        this.aiforced =aiforced;
        this.id = id;
    }


}
class Pool {

    constructor(title, description, options,isForStudent,id,trueid) {
        this.title = title;
        this.description = description;
        this.options = options;
        this.isStudent = isForStudent;
        this.id = id;
        this.trueid = trueid;
    }


}