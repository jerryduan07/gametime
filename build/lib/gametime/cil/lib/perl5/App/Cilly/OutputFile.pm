package App::Cilly::OutputFile;

our @ISA = ();

use strict;
use Carp;
use File::Basename;
use File::Spec;


########################################################################


my $debug = 0;


sub new {
    croak 'bad argument count' unless @_ == 3;
    my ($proto, $basis, $filename) = @_;
    my $class = ref($proto) || $proto;

    $basis = $basis->basis if ref $basis;
    my $ref = { filename => $filename,
		basis => $basis };
    my $self = bless $ref, $class;

    $self->checkRef($filename);
    $self->checkRef($basis);
    $self->checkProtected();
    $self->checkTemporary();

    Carp::cluck "OutputFile: filename == $filename, basis == $basis" if $debug;
    return $self;
}


sub filename {
    my ($self) = @_;
    return $self->{filename};
}


sub basis {
    my ($self) = @_;
    return $self->{basis};
}


########################################################################


sub checkRef {
    my ($self, $filename) = @_;
    confess "ref found where string expected: $filename" if ref $filename;
    confess "stringified ref found where string expected: $filename" if $filename =~ /\w+=HASH\(0x[0-9a-f]+\)/;
}


sub checkTemporary {
    my ($self) = @_;
    my ($basename, $path) = fileparse $self->filename;
    return if $path eq File::Spec->tmpdir . '/';
    confess "found temporary file in wrong directory: ", $self->filename
	if $basename =~ /^cil-[a-zA-Z0-9]{8}\./;
}


########################################################################


my @protected = ();


sub checkProtected {
    my ($self) = @_;
    my $abs = File::Spec->rel2abs($self->filename);

    foreach (@protected) {
	confess "caught attempt to overwrite protected file: ", $self->filename
	    if $_ eq $abs;
    }
}


sub protect {
    my ($self, @precious) = @_;
    push @protected, File::Spec->rel2abs($_)
	foreach @precious;
}


########################################################################


1;

__END__


=head1 Name

OutputFile - base class for intermediate compiler output files

=head1 Description

C<OutputFile> represents an intermediate output file generated by some
stage of a C<Cilly>-based compiler.  This is an abstract base class
and should never be instantiated directly.  It provides common
behaviors used by concrete subclasses L<KeptFile|KeptFile> and
L<TempFile|TempFile>.

=head2 Public Methods

=over

=item filename

An C<OutputFile> instance is a smart wrapper around a file name.  C<<
$out->filename >> returns the name of the file represented by
C<OutputFile> instance C<$out>.  When building a command line, this is
the string to use for the file.  For example:

    my $out = ... ;		# some OutputFile subclass
    my @argv = ('gcc', '-E', '-o', $out->filename, 'input.c');
    system @argv;

C<Cilly> often creates command vectors with a mix of strings and
C<OutputFile> objects.  This is fine, but before using a mixed vector
as a command line, you must replace all C<OutputFile> objects with
their corresponding file names:

    my @mixed = (...);		# mix of strings and objects
    my @normalized = @mixed;
    $_ = (ref $_ ? $_->filename : $_) foreach @normalized;
    system @normalized;

Common utility methods like C<Cilly::runShell> already do exactly this
normalization, but you may need to do it yourself if you are running
external commands on your own.

=item protect

C<OutputFile> contains safety interlocks that help it avoid stomping
on user input files.  C<< OutputFile->protect($precious) >> marks
C<$precious> as a protected input file which should not be
overwritten.  If any C<OutputFile> tries to claim this same file name,
an error will be raised.  In theory, this never happens.  In practice,
scripts can have bugs, and it's better to be safe than sorry.

C<Cilly> uses this method to register input files that it discovers
during command line processing.  If you add special command line
processing of your own, or if you identify input files through other
means, we highly recommend using this method as well.  Otherwise,
there is some risk that a buggy client script could mistakenly create
an output file that destroys the user's source code.

Note that C<protect> is a class method: call it on the C<OutputFile>
module, rather than on a specific instance.

=back

=head2 Internal Methods

The following methods are used within C<OutputFile> or by
C<OutputFile> subclasses.  They are not intended for use by outside
scripts.

=over

=item basis

In addition to L<its own file name|/"filename">, each C<OutputFile>
instance records a second file name: its I<basis>.  The basis file
name is initialized and used differently by different subclasses, but
typically represents the input file from which this output file is
derived.  C<< $out->basis >> returns the basis file name for instance
C<$out>.

When instantiating an C<OutputFile>, the caller can provide either a
file name string as the basis or another C<OutputFile> instance.
However, basis file names are not chained: if C<< $a->basis >> is
F<foo.c>, and C<$b> is constructed with C<$a> as its basis, C<<
$b->basis >> will return F<foo.c>, not C<$a> or C<< $a->filename >>.
This flattening is done at construction time.

See L<KeptFile/"new"> and L<TempFile/"new"> for more details on how
basis file names are used.

=item checkRef

C<< OutputFile->checkRef($filename) >> raises an error if C<$filename>
is an object reference, or looks like the string representation of an
object reference.  Used to sanity check arguments to various methods.

=item checkTemporary

C<< $out->checkTemporary >> raises an error if C<< $out->filename >>
looks like a temporary file name but is not in the system temporary
directory.  Used to sanity check arguments in various methods.

=item checkProtected

C<< $out->checkProtected >> raises an error if C<< $out->filename >>
is listed as a protected file.  This check, performed at construction
time, implements a safety interlock to prevent overwriting of user
input files.  Protected files are registered using L<"protect">.

=back

=head1 See Also

L<KeptFile>, L<TempFile>.

=cut
