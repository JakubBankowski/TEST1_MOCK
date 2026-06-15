namespace TEST1_MOCK.Exceptions;

public class NotFoundException : Exception { public NotFoundException(string msg) : base(msg) {} }
public class BadRequestException : Exception { public BadRequestException(string msg) : base(msg) {} }