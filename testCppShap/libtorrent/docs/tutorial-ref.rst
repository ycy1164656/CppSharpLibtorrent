

.. include:: header.rst

.. contents:: Table of contents
  :depth: 2
  :backlinks: none

tutorial
========

The fundamental feature of starting and downloading torrents in libtorrent is
achieved by creating a *session*, which provides the context and a container for
torrents. This is done with via the `session`__ class, most of its interface is
documented under `session_handle`__ though.

To add a torrent to the `session`__, you fill in an `add_torrent_params`__ object and
pass it either to `add_torrent()`__ or `async_add_torrent()`__.

``add_torrent()`` is a blocking call which returns a `torrent_handle`__.

For example:

.. code:: c++

	#include <libtorrent/session.hpp>
	#include <libtorrent/add_torrent_params.hpp>
	#include <libtorrent/torrent_handle.hpp>
	#include <libtorrent/magnet_uri.hpp>

	int main(int argc, char const* argv[])
	{
		if (argc != 2) {
			fprintf(stderr, "usage: %s <magnet-url>\n");
			return 1;
		}
		lt::session ses;

		lt::add_torrent_params atp = lt::parse_magnet_uri(argv[1]);
		atp.save_path = "."; // save in current dir
		lt::torrent_handle h = ses.add_torrent(atp);

		// ...
	}

Once you have a `torrent_handle`__, you can affect it as well as querying status.
First, let's extend the example to print out messages from the bittorrent engine
about progress and events happening under the hood. libtorrent has a mechanism
referred to as *alerts* to communicate back information to the client application.

Clients can poll a `session`__ for new alerts via the `pop_alerts()`__ call. This
function fills in a vector of `alert`__ pointers with all new alerts since the last
call to this function. The pointers are owned by the `session`__ object at will
become invalidated by the next call to `pop_alerts()`__.

The alerts form a class hierarchy with `alert`__ as the root class. Each specific
kind of `alert`__ may include additional state, specific to the kind of message. All
alerts implement a `message()`__ function that prints out pertinent information
of the `alert`__ message. This can be convenient for simply logging events.

For programmatically react to certain events, use alert_cast to attempt
a down cast of an `alert`__ object to a more specific type.

In order to print out events from libtorrent as well as exiting when the torrent
completes downloading, we can poll the `session`__ for alerts periodically and print
them out, as well as listening for the `torrent_finished_alert`__, which is posted
when a torrent completes.

__ reference-Session.html#session
__ reference-Session.html#session_handle
__ reference-Session.html#session
__ reference-Add_Torrent.html#add_torrent_params
__ reference-Session.html#add_torrent()
__ reference-Session.html#async_add_torrent()
__ reference-Torrent_Handle.html#torrent_handle
__ reference-Torrent_Handle.html#torrent_handle
__ reference-Session.html#session
__ reference-Session.html#pop_alerts()
__ reference-Alerts.html#alert
__ reference-Session.html#session
__ reference-Session.html#pop_alerts()
__ reference-Alerts.html#alert
__ reference-Alerts.html#alert
__ reference-Alerts.html#message()
__ reference-Alerts.html#alert
__ reference-Alerts.html#alert
__ reference-Session.html#session
__ reference-Alerts.html#torrent_finished_alert

.. include:: ../examples/bt-get.cpp
	:code: c++
	:tab-width: 2
	:start-after: */

alert masks
-----------

The output from this program will be quite verbose, which is probably a good
starting point to get some understanding of what's going on. Alerts are
categorized into `alert`__ categories. Each category can be enabled and disabled
independently via the *alert mask*.

The `alert`__ mask is a configuration option offered by libtorrent. There are many
configuration options, see `settings_pack`__. The alert_mask setting is an integer
of the category flags ORed together.

For instance, to only see the most pertinent alerts, the `session`__ can be
constructed like this:

.. code:: c++

	lt::settings_pack pack;
	pack.set_int(lt::settings_pack::alert_mask
		, lt::alert_category::error
		| lt::alert_category::storage
		| lt::alert_category::status);

	lt::session ses(pack);

Configuration options can be updated after the `session`__ is started by calling
`apply_settings()`__. Some settings are best set before starting the `session`__
though, like listen_interfaces, to avoid race conditions. If you start the
`session`__ with the default settings and then immediately change them, there will
still be a window where the default settings apply.

Changing the settings may trigger listen sockets to close and re-open and
NAT-PMP, UPnP updates to be sent. For this reason, it's typically a good idea
to batch settings updates into a single call.

session destruction
-------------------

The `session`__ destructor is blocking by default. When shutting down, trackers
will need to be contacted to stop torrents and other outstanding operations
need to be cancelled. Shutting down can sometimes take several seconds,
primarily because of trackers that are unresponsive (and time out) and also
DNS servers that are unresponsive. DNS lookups are especially difficult to
abort when stalled.

In order to be able to start destruction asynchronously, one can call
`session::abort()`__.

This call returns a `session_proxy`__ object, which is a handle keeping the `session`__
state alive while shutting it down. It deliberately does not provide any of the
`session`__ operations, since it's shutting down.

After having a `session_proxy`__ object, the `session`__ destructor does not block.
However, the `session_proxy`__ destructor *will*.

This can be used to shut down multiple sessions or other parts of the
application in parallel.

asynchronous operations
-----------------------

Essentially any call to a member function of `session`__ or `torrent_handle`__ that
returns a value is a blocking synchronous call. Meaning it will post a message
to the main libtorrent thread and wait for a response. Such calls may be
expensive, and in applications where stalls should be avoided (such as user
interface threads), blocking calls should be avoided.

In the example above, session::add_torrent() returns a `torrent_handle`__ and is
thus blocking. For higher efficiency, `async_add_torrent()`__ will post a message
to the main thread to add a torrent, and post the resulting `torrent_handle`__ back
in an `alert`__ (`add_torrent_alert`__). This is especially useful when adding a lot
of torrents in quick succession, as there's no stall in between calls.

In the example above, we don't actually use the `torrent_handle`__ for anything, so
converting it to use `async_add_torrent()`__ is just a matter of replacing the
`add_torrent()`__ call with `async_add_torrent()`__.

torrent_status_updates
----------------------

To get updates to the status of torrents, call `post_torrent_updates()`__ on the
`session`__ object. This will cause libtorrent to post a `state_update_alert`__
containing `torrent_status`__ objects for all torrents whose status has *changed*
since the last call to `post_torrent_updates()`__.

The `state_update_alert`__ looks something like this:

.. code:: c++

	struct state_update_alert : alert
	{
		virtual std::string message() const;
		std::vector<torrent_status> status;
	};

The ``status`` field only contains the `torrent_status`__ for torrents with
updates since the last call. It may be empty if no torrent has updated its
state. This feature is critical for scalability.

See the `torrent_status`__ object for more information on what is in there.
Perhaps the most interesting fields are ``total_payload_download``,
``total_payload_upload``, ``num_peers`` and ``state``.

resuming torrents
-----------------

Since bittorrent downloads pieces of files in random order, it's not trivial to
resume a partial download. When resuming a download, the bittorrent engine must
restore the state of the downloading torrent, specifically which parts of the
file(s) are downloaded. There are two approaches to doing this:

1. read every piece of the downloaded files from disk and compare it against its
   expected hash.
2. save, to disk, the state of which pieces (and partial pieces) are downloaded,
   and load it back in again when resuming.

If no resume data is provided with a torrent that's added, libtorrent will
employ (1) by default.

To save resume data, call `save_resume_data()`__ on the `torrent_handle`__ object.
This will ask libtorrent to generate the resume data and post it back in
a `save_resume_data_alert`__. If generating the resume data fails for any reason,
a `save_resume_data_failed_alert`__ is posted instead.

Exactly one of those alerts will be posted for every call to
`save_resume_data()`__. This is an important property when shutting down a
`session`__ with multiple torrents, every resume `alert`__ must be handled before
resuming with shut down. Any torrent may fail to save resume data, so the client
would need to keep a count of the outstanding resume files, decremented on
either `save_resume_data_alert`__ or `save_resume_data_failed_alert`__.

The `save_resume_data_alert`__ looks something like this:

.. code:: c++

	struct save_resume_data_alert : torrent_alert
	{
		virtual std::string message() const;

		// the resume data
		add_torrent_params params;
	};

The ``params`` field is an `add_torrent_params`__ object containing all the state
to add the torrent back to the `session`__ again. This object can be serialized
using `write_resume_data()`__ or `write_resume_data_buf()`__, and de-serialized
with `read_resume_data()`__.

example
-------

Here's an updated version of the above example with the following updates:

1. not using blocking calls
2. printing torrent status updates rather than the raw log
3. saving and loading resume files

__ reference-Alerts.html#alert
__ reference-Alerts.html#alert
__ reference-Settings.html#settings_pack
__ reference-Session.html#session
__ reference-Session.html#session
__ reference-Session.html#apply_settings()
__ reference-Session.html#session
__ reference-Session.html#session
__ reference-Session.html#session
__ reference-Session.html#abort()
__ reference-Session.html#session_proxy
__ reference-Session.html#session
__ reference-Session.html#session
__ reference-Session.html#session_proxy
__ reference-Session.html#session
__ reference-Session.html#session_proxy
__ reference-Session.html#session
__ reference-Torrent_Handle.html#torrent_handle
__ reference-Torrent_Handle.html#torrent_handle
__ reference-Session.html#async_add_torrent()
__ reference-Torrent_Handle.html#torrent_handle
__ reference-Alerts.html#alert
__ reference-Alerts.html#add_torrent_alert
__ reference-Torrent_Handle.html#torrent_handle
__ reference-Session.html#async_add_torrent()
__ reference-Session.html#add_torrent()
__ reference-Session.html#async_add_torrent()
__ reference-Session.html#post_torrent_updates()
__ reference-Session.html#session
__ reference-Alerts.html#state_update_alert
__ reference-Torrent_Status.html#torrent_status
__ reference-Session.html#post_torrent_updates()
__ reference-Alerts.html#state_update_alert
__ reference-Torrent_Status.html#torrent_status
__ reference-Torrent_Status.html#torrent_status
__ reference-Torrent_Handle.html#save_resume_data()
__ reference-Torrent_Handle.html#torrent_handle
__ reference-Alerts.html#save_resume_data_alert
__ reference-Alerts.html#save_resume_data_failed_alert
__ reference-Torrent_Handle.html#save_resume_data()
__ reference-Session.html#session
__ reference-Alerts.html#alert
__ reference-Alerts.html#save_resume_data_alert
__ reference-Alerts.html#save_resume_data_failed_alert
__ reference-Alerts.html#save_resume_data_alert
__ reference-Add_Torrent.html#add_torrent_params
__ reference-Session.html#session
__ reference-Resume_Data.html#write_resume_data()
__ reference-Resume_Data.html#write_resume_data_buf()
__ reference-Resume_Data.html#read_resume_data()

.. include:: ../examples/bt-get2.cpp
	:code: c++
	:tab-width: 2
	:start-after: */

session state
-------------

On construction, a `session`__ object is configured by a `session_params`__ object. The
`session_params`__ object notably contain session_settings, the state of the DHT
node (e.g. routing table), the session's IP filter as well as the disk I/O
back-end and dht storage to use.

There are functions to serialize and de-serialize the `session_params`__ object to
help in  restoring `session`__ state from last run. Doing so is especially helpful
for bootstrapping the DHT, using nodes from last run.

Before destructing the `session`__ object, call ``session::session_state()`` to get
the current state as a `session_params`__ object.

Call `write_session_params()`__ or `write_session_params_buf()`__ to serialize the state
into a bencoded `entry`__ or to a flat buffer (``std::vector<char>``) respectively.

On startup, before constructing the `session`__ object, load the buffer back from
disk and call `read_session_params()`__ to de-serialize it back into a `session_params`__
object. Before passing it into the `session`__ constructor is your chance to set
update the `settings_pack`__ (``params``) member of settings_params, or configuring
the disk_io_constructor.

example
-------

Another updated version of the above example with the following updates:

1. load and save `session_params`__ to file ".session"
2. allow shutting down on ``SIGINT``

__ reference-Session.html#session
__ reference-Session.html#session_params
__ reference-Session.html#session_params
__ reference-Session.html#session_params
__ reference-Session.html#session
__ reference-Session.html#session
__ reference-Session.html#session_params
__ reference-Session.html#write_session_params()
__ reference-Session.html#write_session_params_buf()
__ reference-Bencoding.html#entry
__ reference-Session.html#session
__ reference-Session.html#read_session_params()
__ reference-Session.html#session_params
__ reference-Session.html#session
__ reference-Settings.html#settings_pack
__ reference-Session.html#session_params

.. include:: ../examples/bt-get3.cpp
	:code: c++
	:tab-width: 2
	:start-after: */

torrent files
-------------

To add torrent files to a `session`__ (as opposed to a magnet link), it must first
be loaded into a `torrent_info`__ object.

The `torrent_info`__ object can be created either by filename a buffer or a
bencoded structure. When adding by filename, there's a sanity check limit on the
size of the file, for adding arbitrarily large torrents, load the file outside
of the constructor.

The `torrent_info`__ object provides an opportunity to query information about the
.torrent file as well as mutating it before adding it to the `session`__.

bencoding
---------

bencoded structures is the default data storage format used by bittorrent, such
as .torrent files, tracker announce and scrape responses and some wire protocol
extensions. libtorrent provides an efficient framework for decoding bencoded
data through `bdecode()`__ function.

There are two separate mechanisms for *encoding* and *decoding*. When decoding,
use the `bdecode()`__ function that returns a `bdecode_node`__. When encoding, use
`bencode()`__ taking an `entry`__ object.

The key property of `bdecode()`__ is that it does not copy any data out of the
buffer that was parsed. It builds the tree structures of references pointing
into the buffer. The buffer must stay alive and valid for as long as the
`bdecode_node`__ is in use.

For performance details on `bdecode()`__, see the `blog post`_ about it.

.. _`blog post`: https://blog.libtorrent.org/2015/03/bdecode-parsers/

 
__ reference-Session.html#session
__ reference-Torrent_Info.html#torrent_info
__ reference-Torrent_Info.html#torrent_info
__ reference-Torrent_Info.html#torrent_info
__ reference-Session.html#session
__ reference-Bdecoding.html#bdecode()
__ reference-Bdecoding.html#bdecode()
__ reference-Bdecoding.html#bdecode_node
__ reference-Bencoding.html#bencode()
__ reference-Bencoding.html#entry
__ reference-Bdecoding.html#bdecode()
__ reference-Bdecoding.html#bdecode_node
__ reference-Bdecoding.html#bdecode()

